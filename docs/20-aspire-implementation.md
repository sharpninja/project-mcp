---
title: Aspire Orchestration — Implementation Plan
---

# Aspire Orchestration — Excruciatingly Detailed Implementation Plan

This document provides an **excruciatingly detailed** implementation plan for **.NET Aspire** orchestration of the Project MCP application. The App Host composes the MCP server and PostgreSQL (and optionally the web app), injects connection strings and configuration, and provides a single entry point for local and containerized runs. See [07 — Deployment](07-deployment.html), [13 — Deployment Plan](13-deployment-plan.html), and [06 — Tech Requirements](06-tech-requirements.html).

**References:** [02 — Architecture](02-architecture.html), [07 — Deployment](07-deployment.html), [11 — Implementation Plan](11-implementation-plan.html), [13 — Deployment Plan](13-deployment-plan.html).

---

## 1. Goals and scope

### 1.1 Goals

- **Single entry point:** Run the entire stack (PostgreSQL + MCP server, and optionally web app) with `dotnet run --project AppHost` (or equivalent).
- **Configuration injection:** Connection strings and environment variables are supplied by the App Host to the MCP server (and web app) so that **no secrets or connection strings are hardcoded** in the server project.
- **Containerized PostgreSQL:** PostgreSQL runs as a container managed by Aspire (e.g. `AddPostgresContainer`); the MCP server runs as a .NET process or container and connects to that Postgres instance.
- **Dashboard (optional):** Use Aspire Dashboard in development for logs, metrics, and resource listing.
- **Docker support:** The MCP server can be built as a Docker image; the App Host can reference the server via `AddProject()` (run as process) or `AddDockerfile()` (run as container) so that the same solution supports both “run as process” and “run in container” modes.

### 1.2 Out of scope (v1)

- Kubernetes or other orchestrators.
- Production cloud-specific Aspire extensions (e.g. Azure Container Apps); design assumes Aspire + Docker as the deployment model.

---

## 2. Solution structure

### 2.1 Projects in the solution

| Project | Type | Purpose |
|---------|------|---------|
| **ProjectMcp.AppHost** | Aspire App Host | Orchestrator; references hosting package and declares resources (Postgres, MCP server, optional web app). |
| **ProjectMcp.Server** | Console / Web | MCP server (stdio + optional REST). References TodoEngine and MCP SDK. |
| **ProjectMcp.TodoEngine** | Class library | TODO domain and data access (see [19 — TODO Library](19-todo-library-implementation.html)). |
| **ProjectMcp.WebApp** (optional, Phase 10) | Blazor Web App | Web UI; same solution, added in Phase 10. |

**Step 2.1.1:** Create the App Host project with `dotnet new aspire-apphost -n ProjectMcp.AppHost` (or equivalent template). If the template is not available, create a .NET project that references `Aspire.Hosting.AppHost` (13.1.0) and uses the hosting APIs.

### 2.2 App Host project file

- **Target framework:** net10.0 (or the same as the MCP server).
- **Package references:** 
  - `Aspire.Hosting.AppHost` (13.1.0) — core hosting.
  - `Aspire.Hosting.PostgreSQL` (13.1.0) — Postgres resource (`AddPostgres`, `AddDatabase`).
- **Project references:** 
  - `ProjectMcp.Server` — so the App Host can add the server as a project resource.
  - Optionally `ProjectMcp.WebApp` when it exists.

**Step 2.2.1:** Do **not** reference TodoEngine from the App Host; the App Host only references the executable projects (Server, WebApp). The Server project references TodoEngine.

---

## 3. Program.cs (App Host) — step-by-step

### 3.1 Builder and distributed application

- **Step 3.1.1:** In `Program.cs` of the App Host, create the builder:
  - `var builder = DistributedApplication.CreateBuilder(args);`
  - Use the standard Aspire pattern so that the rest of the setup uses `builder`.

### 3.2 PostgreSQL resource

- **Step 3.2.1:** Add a PostgreSQL container:
  - `var postgres = builder.AddPostgres("postgres");`
  - Optional parameters: `userName`, `password`, and `port` (host port).
- **Step 3.2.2:** Add a database (name defaults to the resource name if not provided):
  - `var projectDb = postgres.AddDatabase("projectmcp");`
- **Step 3.2.3:** If a fixed host port is required for local tools, pass `port` to `AddPostgres`. Otherwise let Aspire assign a random port and rely on connection string injection.

### 3.3 MCP server resource

- **Step 3.3.1:** Add the MCP server as a **project** (run as process):
  - `var mcpServer = builder.AddProject<Projects.ProjectMcp_Server>("mcpserver");`
  - Use the correct project type name (e.g. `Projects.ProjectMcp_Server` is the generated type for the Server project). This makes the server run as a child process when the App Host runs.
- **Step 3.3.2:** Inject the Postgres connection string into the MCP server:
  - `mcpServer.WithReference(projectDb);` (injects `ConnectionStrings__projectmcp` by default).
- **Step 3.3.3:** If the server expects a specific key (e.g. `ConnectionStrings__DefaultConnection`), use the overload with `connectionName`:
  - `mcpServer.WithReference(projectDb, connectionName: "DefaultConnection");`
- **Step 3.3.4:** Set environment variables for the server if needed:
  - e.g. `mcpServer.WithEnvironment("PROJECT_MCP_HTTP_ENABLED", "true")` and `PROJECT_MCP_HTTP_PORT`, `5000` so that when run under App Host, the server listens on HTTP. Optionally pass `ASPNETCORE_URLS` if the server uses that.
- **Step 3.3.5:** If the server exposes an HTTP endpoint, Aspire can use it for health checks and dashboard. Configure the server’s endpoint (e.g. port 5000) so that the App Host knows the server’s URL (e.g. for “Open dashboard” or service discovery). This may be automatic when using `AddProject` if the project uses `WebApplication`.

### 3.4 Optional: MCP server as Docker container

- **Step 3.4.1:** To run the MCP server as a **container** instead of a process, add a Dockerfile to the Server project (multi-stage: build with SDK, run with runtime image). Then in the App Host:
  - `var mcpServer = builder.AddDockerfile("mcpserver", "path/to/Server");` (optional parameters: `dockerfilePath`, `stage`).
- **Step 3.4.2:** Pass the Postgres connection string to the container via environment variable. The Postgres container’s host/port in the connection string must be reachable from the MCP container (e.g. use Aspire’s service name for the Postgres host). Aspire typically sets this when you use `WithReference(postgres)` on the container resource.
- **Step 3.4.3:** Document that “run as process” is the default for local dev and “run as container” is for CI or production-like local runs.

### 3.5 Web app (Phase 10)

- When the web app project exists: add it with `builder.AddProject<Projects.ProjectMcp_WebApp>("webapp")` and inject the Postgres connection string and any API base URL (e.g. MCP server’s REST URL) if the web app calls the MCP API. The web app will use the same database as the MCP server.

### 3.6 Build and run

- **Step 3.6.1:** Call `builder.Build().Run()` (or the equivalent) at the end of `Program.cs` so that the distributed application starts and runs until shutdown.

---

## 4. Configuration injection (exact keys)

### 4.1 What the App Host must provide to the MCP server

| Configuration key | Source | Purpose |
|-------------------|--------|---------|
| **Connection string** | Postgres resource reference | EF Core and TodoEngine use this to connect to PostgreSQL. |
| **PROJECT_MCP_HTTP_ENABLED** (optional) | Env or App Host env | If "true", enable Kestrel. |
| **PROJECT_MCP_HTTP_PORT** or **ASPNETCORE_URLS** | Env or App Host env | Port or URLs for HTTP. |
| **PROJECT_MCP_ENTERPRISE_ID** (optional) | Env or App Host env | Default enterprise for scope. |
| **PROJECT_MCP_PROJECT_ID** (optional) | Env or App Host env | Default project for scope. |
| **PROJECT_MCP_ROOT** (optional) | Env | Project root path for file-based concerns. |

**Step 4.1.1:** In the MCP server project, read the connection string from (in order of precedence): (1) environment variable `PROJECT_MCP_CONNECTION_STRING`, (2) `DATABASE_URL`, (3) `ConnectionStrings__DefaultConnection` (from IConfiguration). The App Host, when using `WithReference(postgres)`, should set one of these so the server does not need to be configured manually.

**Step 4.1.2:** Document in README that when running via App Host, the connection string is injected automatically; when running the server standalone (e.g. for Cursor stdio), the user must set the connection string in the environment.

### 4.2 No secrets in repo

- **Step 4.2.1:** The App Host must **not** contain connection strings or passwords. The Postgres container may use a default password for local dev (e.g. Aspire’s default); that password is generated or set by Aspire and injected via reference. For production, document that secrets (e.g. Postgres password) are supplied via environment or secret store outside the repo.

---

## 5. PostgreSQL container (detailed)

### 5.1 Image and version

- **Image:** Use the official Postgres image (e.g. `postgres:16` or the image specified by the Aspire PostgreSQL extension). The Aspire API may abstract the image name.
- **Step 5.1.1:** Set the Postgres version in the AddPostgresContainer call if the API supports it (e.g. `AddPostgresContainer("postgres", "16")`). Use a fixed version for reproducible builds.

### 5.2 Database name and user

- **Step 5.2.1:** Create a named database (e.g. `projectmcp`) so the connection string points to that database. The server will run migrations against this database.
- **Step 5.2.2:** Ensure the connection string includes username and password. Aspire typically generates a random password and passes it to the server via the reference; no need to hardcode.

### 5.3 Port (optional)

- For local debugging with an external SQL client, you may want to expose the Postgres port on the host. The Aspire API may support `.WithHostPort(5432)` or similar. Document that this is optional and may conflict with a local Postgres install.

---

## 6. Dashboard and observability

### 6.1 Aspire Dashboard

- **Step 6.1.1:** By default, the App Host may launch the Aspire Dashboard (a web UI) that shows resources, logs, and metrics. Ensure the dashboard is enabled in development (e.g. `builder.AddDashboard()` or the default when using the App Host template).
- **Step 6.1.2:** Document the dashboard URL (e.g. https://localhost:15231 or the port shown at startup). Users can open it to see the MCP server and Postgres resource, view logs, and trace requests.

### 6.2 OpenTelemetry (optional)

- **Step 6.2.1:** If the MCP server and Aspire support OpenTelemetry, add the appropriate package to the server (e.g. `OpenTelemetry.Instrumentation.AspNetCore`) and configure the exporter to send traces/metrics to the Aspire dashboard or a collector. This is optional for v1; the plan only requires that the App Host can start the server and inject config.

---

## 7. Docker and Dockerfile (MCP server)

### 7.1 Dockerfile location and structure

- **Location:** Place the Dockerfile in the **Server** project directory (e.g. `src/ProjectMcp.Server/Dockerfile`) or at the solution root with a build context that includes the Server and TodoEngine projects.
- **Step 7.1.1:** Multi-stage build:
  - **Stage 1 (build):** Use `mcr.microsoft.com/dotnet/sdk:10.0` (or current .NET version). Copy solution and project files; run `dotnet restore` and `dotnet publish -c Release -o /app/publish` for the Server project.
  - **Stage 2 (run):** Use `mcr.microsoft.com/dotnet/aspnet:10.0` (or `runtime:10.0` if the server is console-only). Copy `/app/publish` from the build stage. Set `ENTRYPOINT ["dotnet", "ProjectMcp.Server.dll"]` (or the actual output name).
- **Step 7.1.2:** Expose the HTTP port (e.g. `EXPOSE 5000`) so that when the container runs, the REST endpoint is reachable. Document that the server must be started with HTTP enabled (e.g. env `ASPNETCORE_URLS=http://+:5000` or `PROJECT_MCP_HTTP_PORT=5000`).

### 7.2 Environment variables in container

- **Step 7.2.1:** The container must receive the connection string at runtime (from Aspire or from the orchestrator). Do not embed it in the image. Use `ENV` only for non-secret defaults (e.g. `PROJECT_MCP_HTTP_PORT=5000`). Connection string and secrets are injected when the container is run.

---

## 8. Runtime flow (end-to-end)

### 8.1 Sequence

1. **Developer runs:** `dotnet run --project ProjectMcp.AppHost` (or from the App Host directory).
2. **Aspire starts:** Postgres container is started (if not already running). MCP server process (or container) is started.
3. **Connection string:** Aspire passes the Postgres connection string (host = container name or localhost with mapped port, database, user, password) to the MCP server via the configured reference.
4. **Server startup:** The MCP server reads the connection string from configuration, runs EF Core migrations on startup, and starts listening (stdio and/or HTTP).
5. **Dashboard:** User can open the Aspire Dashboard to see resources and logs.
6. **Shutdown:** Ctrl+C or stop command stops the App Host, which tears down the server process and the Postgres container (depending on Aspire lifecycle).

### 8.2 Migrations

- **Step 8.2.1:** Run migrations as part of server startup (e.g. `context.Database.Migrate()` in Program.cs). Ensure the server starts only after Postgres is ready (Aspire may wait for the container to be healthy).

---

## 9. Health checks (optional)

- **Step 9.1:** Add a health check endpoint to the MCP server (e.g. `/health` that returns 200 if the server can connect to the database). Aspire or the dashboard can use this to show “healthy” status. Implementation: use `AddHealthChecks().AddDbContextCheck<TodoEngineDbContext>()` and `MapHealthChecks("/health")` in the server.

---

## 10. Task checklist (summary)

| ID | Task | Dependency |
|----|------|------------|
| A.1 | Create App Host project; add Aspire.Hosting.AppHost and Aspire.Hosting.PostgreSQL (or current package names) | — |
| A.2 | Add Postgres container with named database; obtain connection string reference | A.1 |
| A.3 | Add MCP server as project resource; inject Postgres connection string reference | A.2, Server project |
| A.4 | Set server env vars (HTTP enabled, port) when run under App Host | A.3 |
| A.5 | Verify server reads connection string from config (ConnectionStrings__DefaultConnection or env) | Phase 1 |
| A.6 | Add Dockerfile for MCP server (multi-stage, expose port); document env injection at runtime | — |
| A.7 | Optional: Add MCP server as Docker container in App Host; pass connection string to container | A.6 |
| A.8 | Document: how to run App Host, dashboard URL, how to run server standalone for Cursor | A.3 |
| A.9 | Optional: Add health check to server and confirm dashboard shows status | Phase 7 |
| A.10 | Optional: Add web app project to App Host when it exists (Phase 10) | Phase 10 |

---

## 11. Dependencies on other phases

- **Phase 0:** App Host and Server projects exist; Server has no DB yet. App Host can still start the server and Postgres; server will fail on first DB use until Phase 1.
- **Phase 1:** Server and TodoEngine use the injected connection string; migrations apply to the Postgres database created by Aspire.
- **Phase 2b:** REST endpoint is available when the server runs under App Host with HTTP enabled; clients can call initialize and tools.
- **Phase 10:** Web app can be added as another project resource in the App Host, sharing the same Postgres.

This document is the single reference for implementing Aspire orchestration in excruciating detail.
