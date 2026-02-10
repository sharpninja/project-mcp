using EFCore.NamingConventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Data;
using ProjectMCP.TodoEngine.Repositories;
using ProjectMCP.TodoEngine.Services;
using ProjectMCP.TodoEngine.Views;

namespace ProjectMCP.TodoEngine;

public static class TodoEngineServiceCollectionExtensions
{
    public static IServiceCollection AddTodoEngine(this IServiceCollection services, Action<TodoEngineOptions>? configure = null)
    {
        var options = new TodoEngineOptions();
        configure?.Invoke(options);

        if (options.Provider.HasValue && !string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            services.AddDbContext<TodoEngineDbContext>(dbOptions =>
            {
                switch (options.Provider.Value)
                {
                    case TodoEngineProvider.Postgres:
                        dbOptions
                            .UseNpgsql(options.ConnectionString, npgsql =>
                                npgsql.MigrationsAssembly(typeof(TodoEngineDbContext).Assembly.GetName().Name))
                            .UseSnakeCaseNamingConvention();
                        break;
                    case TodoEngineProvider.Sqlite:
                        dbOptions.UseSqlite(options.ConnectionString);
                        break;
                    case TodoEngineProvider.SqlServer:
                        dbOptions.UseSqlServer(options.ConnectionString);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(options.Provider), options.Provider, "Unsupported provider.");
                }
            });
        }

        services.AddScoped<ISlugService, SlugService>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IWorkItemRepository, WorkItemRepository>();
        services.AddScoped<IMilestoneRepository, MilestoneRepository>();
        services.AddScoped<IReleaseRepository, ReleaseRepository>();
        services.AddScoped<IResourceRepository, ResourceRepository>();
        services.AddScoped<IProjectResourceRepository, ProjectResourceRepository>();
        services.AddScoped<IRequirementRepository, RequirementRepository>();
        services.AddScoped<IStandardRepository, StandardRepository>();
        services.AddScoped<IIssueRepository, IssueRepository>();
        services.AddScoped<IKeywordRepository, KeywordRepository>();
        services.AddScoped<IDomainRepository, DomainRepository>();
        services.AddScoped<ISystemRepository, SystemRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<ITodoView, TodoView>();

        return services;
    }
}
