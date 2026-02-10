using EFCore.NamingConventions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using ProjectMcp.WebApp.Components;
using ProjectMcp.WebApp.Services;
using Microsoft.EntityFrameworkCore;
using ProjectMCP.TodoEngine;
using ProjectMCP.TodoEngine.Data;
using Serilog;
using Serilog.Formatting.Compact;

// One-off: verify keywords and requirements in the database (uses same config as the app).
// Ingestion commits each row (SaveChangesAsync per Add/Update); no uncommitted transaction.
// To see ingested data, verify-db must connect to the SAME database the webapp uses when you ingest.
// - If you run "AppHost -- verify-db", Aspire starts a NEW Postgres for that run (often empty).
// - To verify the DB you actually ingested into: keep AppHost running (with webapp), ingest in browser,
//   then run verify-db with that run's connection string (from Aspire dashboard or env), e.g.:
//   dotnet run --project src/ProjectMcp.WebApp --no-launch-profile -- verify-db "Host=localhost;Port=XXXX;Database=projectmcp;Username=postgres;Password=..."
// To use Aspire's connection string in a single run: dotnet run --project src/ProjectMcp.AppHost -- verify-db
// Standalone: dotnet run --project src/ProjectMcp.WebApp --no-launch-profile -- verify-db
if (args.Length > 0 && args[0] == "verify-db")
{
    var connectionString = args.Length > 1 ? args[1] : null;
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .AddUserSecrets(typeof(Program).Assembly, optional: true);
        var config = configBuilder.Build();
        connectionString = config["PROJECT_MCP_CONNECTION_STRING"]
            ?? config["DATABASE_URL"]
            ?? config.GetConnectionString("DefaultConnection");
    }
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.WriteLine("ERROR: Set PROJECT_MCP_CONNECTION_STRING, DATABASE_URL, or ConnectionStrings:DefaultConnection.");
        Console.WriteLine("Or pass the connection string as the second argument: verify-db \"Host=...;Database=...;Username=...;Password=...\"");
        Environment.Exit(1);
    }
    var optionsBuilder = new DbContextOptionsBuilder<TodoEngineDbContext>();
    optionsBuilder.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
    const int maxAttempts = 10;
    const int delayMs = 2000;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var db = new TodoEngineDbContext(optionsBuilder.Options);
            var pending = db.Database.GetPendingMigrations().ToList();
            if (pending.Count > 0)
                Console.WriteLine("Applying {0} pending migration(s): {1}", pending.Count, string.Join(", ", pending));
            db.Database.Migrate();
            db.Database.EnsureSchema();
            var keywords = await db.Keywords.OrderBy(k => k.EnterpriseId).ThenBy(k => k.DisplayId).ToListAsync();
            var requirements = await db.Requirements.Include(r => r.Keyword).OrderBy(r => r.ProjectId).ThenBy(r => r.DisplayId).ToListAsync();
            Console.WriteLine("=== Keywords (" + keywords.Count + ") ===");
            foreach (var k in keywords)
                Console.WriteLine("  Id: " + k.Id + "  EnterpriseId: " + k.EnterpriseId + "  DisplayId: " + k.DisplayId + "  Name: " + k.Name);
            Console.WriteLine("=== Requirements (" + requirements.Count + ") ===");
            foreach (var r in requirements)
                Console.WriteLine("  Id: " + r.Id + "  ProjectId: " + r.ProjectId + "  DisplayId: " + r.DisplayId + "  Title: " + (r.Title.Length > 50 ? r.Title[..50] + "…" : r.Title) + "  KeywordId: " + (r.KeywordId?.ToString() ?? "null") + (r.Keyword != null ? " (" + r.Keyword.Name + ")" : ""));
            Environment.Exit(0);
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            var isTransient = ex is Npgsql.NpgsqlException
                || ex.InnerException is Npgsql.NpgsqlException
                || ex.InnerException is System.IO.EndOfStreamException
                || (ex is InvalidOperationException && (ex.Message?.Contains("transient", StringComparison.OrdinalIgnoreCase) == true));
            if (isTransient)
            {
                Console.WriteLine("Connection attempt {0}/{1} failed (transient). Retrying in {2}ms...", attempt, maxAttempts, delayMs);
                await Task.Delay(delayMs);
            }
            else
                throw;
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    var endpoint = context.Configuration["Parseable:Endpoint"] ?? "http://localhost:8000";
    var logStream = context.Configuration["Parseable:LogStream"] ?? "projectmcp";
    var requestUri = $"{endpoint.TrimEnd('/')}/api/v1/logstream/{logStream}/ingest";

    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Async(sink => sink.Console(new RenderedCompactJsonFormatter()))
        .WriteTo.Async(sink => sink.Http(requestUri, queueLimitBytes: null, textFormatter: new RenderedCompactJsonFormatter()));
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = "GitHub";
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddOAuth("GitHub", options =>
    {
        options.ClientId = builder.Configuration["GitHub:ClientId"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["GitHub:ClientSecret"] ?? string.Empty;
        options.CallbackPath = builder.Configuration["GitHub:CallbackPath"] ?? "/signin-github";
        options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
        options.TokenEndpoint = "https://github.com/login/oauth/access_token";
        options.UserInformationEndpoint = "https://api.github.com/user";
        options.SaveTokens = true;
        options.Scope.Add("read:user");
        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);
                request.Headers.UserAgent.ParseAdd("ProjectMcp.WebApp");
                var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();
                using var payload = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
                var root = payload.RootElement;
                var id = root.TryGetProperty("id", out var idProperty) ? idProperty.GetInt64().ToString() : null;
                var login = root.TryGetProperty("login", out var loginProperty) ? loginProperty.GetString() : null;

                if (!string.IsNullOrWhiteSpace(id) && context.Identity is not null)
                {
                    context.Identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, id));

                    var sudoIds = builder.Configuration["PROJECT_MCP_SUDO_GITHUB_IDS"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        ?? Array.Empty<string>();
                    if (sudoIds.Contains(id))
                    {
                        context.Identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "SUDO"));
                    }
                }

                if (!string.IsNullOrWhiteSpace(login) && context.Identity is not null)
                {
                    context.Identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, login));
                }
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.Configure<ProjectMcp.WebApp.Services.ScopeOptions>(builder.Configuration.GetSection(ProjectMcp.WebApp.Services.ScopeOptions.SectionName));

builder.Services.AddTodoEngine(options =>
{
    var connectionString = builder.Configuration["PROJECT_MCP_CONNECTION_STRING"]
        ?? builder.Configuration["DATABASE_URL"]
        ?? builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Database connection string is required.");
    }

    options.ConnectionString = connectionString;
    options.Provider = TodoEngineProvider.Postgres;
});

builder.Services.AddScoped<IUserScopeService, UserScopeService>();
builder.Services.AddScoped<ICurrentSelectionService, CurrentSelectionService>();
builder.Services.AddScoped<IEnterpriseService, EnterpriseService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IRequirementService, RequirementService>();
builder.Services.AddScoped<IStandardService, StandardService>();
builder.Services.AddScoped<IWorkItemService, WorkItemService>();
builder.Services.AddScoped<IMilestoneService, MilestoneService>();
builder.Services.AddScoped<IReleaseService, ReleaseService>();
builder.Services.AddScoped<IIssueService, IssueService>();
builder.Services.AddScoped<IDomainService, DomainService>();
builder.Services.AddScoped<ISystemService, SystemService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddScoped<IKeywordService, KeywordService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IReportsService, ReportsService>();
builder.Services.AddScoped<IGanttService, GanttService>();
builder.Services.AddScoped<IRequirementIngestService, RequirementIngestService>();
builder.Services.AddScoped<IStandardIngestService, StandardIngestService>();
builder.Services.AddSingleton<IAppVersionService, AppVersionService>();

var app = builder.Build();
Log.Information("WebApp starting");

// Apply pending EF Core migrations so base tables exist before handling requests.
// Retry when running under Aspire: Postgres may not be ready yet (connection stream errors).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoEngineDbContext>();
    const int maxAttempts = 15;
    const int delayMs = 2000;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            var pending = db.Database.GetPendingMigrations().ToList();
            if (pending.Count > 0)
                Log.Information("Applying {Count} pending migration(s): {Migrations}.", pending.Count, string.Join(", ", pending));
            db.Database.Migrate();
            // When migration discovery fails (e.g. context in referenced assembly), run schema bootstrap.
            db.Database.EnsureSchema();
            if (attempt > 1)
                Log.Information("Migrations applied after {Attempt} attempt(s).", attempt);
            break;
        }
        catch (Npgsql.NpgsqlException ex) when (attempt < maxAttempts)
        {
            Log.Warning(ex, "Database not ready (attempt {Attempt}/{Max}). Retrying in {Delay}ms.", attempt, maxAttempts, delayMs);
            Thread.Sleep(delayMs);
        }
    }

    // Verify schema: ensure resources table exists so we fail fast with a clear error.
    try
    {
        _ = db.Resources.Any(); // simple query to force table existence check
    }
    catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
    {
        var applied = db.Database.GetAppliedMigrations().ToList();
        throw new InvalidOperationException(
            "The 'resources' table does not exist after migrations. Applied: " +
            (applied.Count > 0 ? string.Join(", ", applied) : "none") +
            ". Do a clean rebuild (dotnet clean && dotnet build). If the DB was created before InitialCreate existed, drop the database or run: DELETE FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '20260209180000_InitialCreate'; then restart.",
            ex);
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
