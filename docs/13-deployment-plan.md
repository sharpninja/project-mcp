---
title: Deployment Plan
---

# Deployment Plan

This document provides a **detailed deployment plan** for the Software Project Management MCP server. It covers local development, Docker, Aspire orchestration, CI/CD, configuration, rollback, and operational checks. It aligns with [07 — Deployment](07-deployment.html), [06 — Tech Requirements](06-tech-requirements.html), and [05 — Tech and Implementation](05-tech-and-implementation.html).

---

## 1. Deployment overview

| Environment | Purpose | How deployed | Database |
|-------------|---------|--------------|----------|
| **Local** | Development and manual testing | `dotnet run` or IDE | Local Postgres (Docker or native) |
| **Local (Aspire)** | Full stack locally with orchestration | App Host (`dotnet run --project AppHost`) | Aspire-managed Postgres container |
| **CI** | Build, test, publish artifacts | GitHub Actions (or similar) | Testcontainers or service container |
| **Staging / pre-prod** | Integration and E2E before release | Containers (Docker Compose or Aspire) | Dedicated Postgres instance |
| **Production** | Live MCP server for end users | Containers; optional orchestration | Production Postgres |

The MCP server is **stdio-based** for v1. When run in a container, the host (e.g. IDE or a sidecar) must attach to the container’s stdin/stdout or the server must expose a transport (e.g. HTTP) that the host can call. This plan assumes **local/Aspire** runs the server as a process with stdio; **container** deployment may use the same process or a thin wrapper that forwards stdio from a network endpoint (implementation choice).

---

## 2. Prerequisites

| Prerequisite | Local | CI | Staging / Prod |
|--------------|--------|-----|-----------------|
| .NET 8 SDK | Required | Required (setup step) | Not needed (runtime image) |
| Docker | Optional (for Postgres or full stack) | Required (containers, Testcontainers) | Required |
| Docker Compose | Optional | Optional | Optional |
| .NET Aspire workload | Optional (for App Host) | Optional | Optional |
| PostgreSQL | Required (local instance or container) | Provided by Testcontainers or service | Dedicated instance or container |
| Git | Required | Required | N/A |

---

## 3. Local development deployment

### 3.1 Database

1. **Install Postgres** (native or Docker):
   - Docker: `docker run -d --name projectmcp-pg -e POSTGRES_PASSWORD=dev -e POSTGRES_DB=projectmcp -p 5432:5432 postgres:16-alpine`
   - Or use a local Postgres instance and create database `projectmcp`.

2. **Connection string:**
   - Format: `Host=localhost;Port=5432;Database=projectmcp;Username=postgres;Password=dev` (or your credentials).
   - Set env: `export DATABASE_URL="Host=localhost;..."` or `export ConnectionStrings__DefaultConnection="..."` or `PROJECT_MCP_CONNECTION_STRING`.

3. **Migrations:**
   - From repo root: `dotnet ef database update --project src/ProjectMcp.Server` (or your server project path).
   - Or run migrations programmatically on startup (optional for dev).

### 3.2 Project root (optional)

- Optional: `export PROJECT_MCP_ROOT=/path/to/your/repo` (or leave unset to use process cwd) for any file-based operations.

### 3.3 Default scope (optional)

- To avoid calling scope_set every time: `export PROJECT_MCP_SCOPE_SLUG=<slug>` (e.g. `E1-P001`).

### 3.4 Run the server

```bash
cd /path/to/ProjectMcp
dotnet run --project src/ProjectMcp.Server
```

- Server reads stdio; Cursor (or other MCP client) launches this command and attaches stdin/stdout.
- **Cursor config example:** Add a server entry with command `dotnet` and args `run --project /path/to/ProjectMcp/src/ProjectMcp.Server` (or use published path); set env in config if needed.

### 3.5 Verification

- Server prints nothing to stdout except MCP protocol messages (or minimal bootstrap log if configured).
- From Cursor: tools and resources appear; scope_set (scope_slug) and project_get_info succeed after setting scope or using default.

---

## 4. Local deployment with Aspire

### 4.1 App Host setup

- App Host project references the MCP server project and Aspire.Hosting.
- In `Program.cs` (App Host):
  - Add Postgres: `var postgres = builder.AddPostgresContainer("postgres").WithEndpoint(port: 5432, targetPort: 5432);` (or use AddPostgres with connection string).
  - Add MCP server: `builder.AddProject<Projects.ProjectMcp_Server>("mcpserver")` (or AddDockerfile if server is containerized).
  - Add connection string to MCP server: e.g. `builder.Configuration["ConnectionStrings__DefaultConnection"] = postgres.GetConnectionString();` or use Aspire’s service discovery to inject connection string into server config.

### 4.2 Run with Aspire

```bash
dotnet run --project src/ProjectMcp.AppHost
```

- Aspire starts the Postgres container and the MCP server (as process or container).
- Dashboard (if enabled): view logs and resources.
- **MCP client:** The client must connect to the MCP server’s stdio. If the server runs as a process child of the App Host, the IDE typically does **not** attach to that process. So for local dev with Cursor, running the **server directly** (`dotnet run --project Server`) with a local Postgres is usually simpler; use Aspire for “full stack in one command” and integration tests.

### 4.3 Verification

- Postgres container is running; MCP server process starts and receives connection string.
- Server can connect to Postgres (e.g. run a tool that hits the DB).

---

## 5. Docker build and run

### 5.1 Dockerfile (MCP server)

- **Location:** `src/ProjectMcp.Server/Dockerfile` or repo root with context.
- **Multi-stage:**
  - Stage 1: Build with SDK image (`mcr.microsoft.com/dotnet/sdk:8.0`). Restore, publish: `dotnet publish -c Release -o /app`.
  - Stage 2: Runtime image (`mcr.microsoft.com/dotnet/aspnet:8.0` or `runtime:8.0`). Copy `/app` from build. Entrypoint: run the published executable (e.g. `dotnet ProjectMcp.Server.dll` or the executable name).
- **No HTTP server** required for stdio; if the container is used with a host that attaches to stdio, ensure the process is the main process (PID 1 or launched by entrypoint) and uses stdin/stdout.

Example (conceptual):

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ProjectMcp.Server/ProjectMcp.Server.csproj", "ProjectMcp.Server/"]
RUN dotnet restore "ProjectMcp.Server/ProjectMcp.Server.csproj"
COPY . .
WORKDIR "/src/ProjectMcp.Server"
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ProjectMcp.Server.dll"]
```

### 5.2 Build and run (standalone container)

```bash
docker build -t projectmcp-server:latest -f src/ProjectMcp.Server/Dockerfile .
docker run --rm -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;..." projectmcp-server:latest
```

- Connection string must reach Postgres (use `host.docker.internal` from container to host Postgres, or run Postgres in another container and use Docker network).
- For stdio: the host that runs `docker run` would typically attach to the container’s stdio (`docker run -it ...`); MCP clients that launch the server may need to run the container with `-i` and attach stdin/stdout.

### 5.3 Docker Compose (server + Postgres)

- **File:** `docker-compose.yml` (or `compose.yaml`) in repo root.
- **Services:**
  - **postgres:** image `postgres:16-alpine`; env `POSTGRES_PASSWORD`, `POSTGRES_DB`; port `5432`; volume for persistence (optional).
  - **mcpserver:** build from Dockerfile; env `ConnectionStrings__DefaultConnection` from Compose (e.g. `Host=postgres;Port=5432;Database=projectmcp;Username=postgres;Password=${POSTGRES_PASSWORD}`); depends_on: postgres.
- **Run:** `docker compose up --build`. Server connects to Postgres on the Compose network.
- **Usage:** For stdio, a client would run `docker compose run mcpserver` and attach to its stdio, or use a sidecar that forwards stdio to a network endpoint (out of scope for this plan).

---

## 6. CI/CD pipeline

### 6.1 Pipeline stages (e.g. GitHub Actions)

| Stage | Steps | Triggers |
|-------|--------|----------|
| **Build** | Checkout; .NET restore; build; (optional) dotnet format / lint | Push, PR |
| **Test** | Run unit tests; run integration tests (Testcontainers or Postgres service); optionally E2E | Push, PR |
| **Publish** | `dotnet publish -c Release`; publish artifact (e.g. server zip or Docker image) | Push to main, or tag |
| **Docker** | Build Docker image; push to registry (e.g. GHCR) | Tag (e.g. v1.0.0) or main |
| **Deploy (staging)** | Deploy to staging (e.g. pull image, run with staging env) | Manual or auto on main |
| **Deploy (production)** | Deploy to production (manual approval, then deploy) | Manual on tag or release |

### 6.2 Example GitHub Actions (concise)

- **Build and test job:**
  - `actions/checkout@v4`
  - `actions/setup-dotnet@v4` (dotnet-version: 8)
  - `dotnet restore`; `dotnet build --no-restore`
  - `dotnet test --no-build --verbosity normal` (unit + integration; ensure Testcontainers or Postgres service if needed)
  - Optionally: coverage report and upload

- **Publish job** (on main or tag):
  - Build and test as above
  - `dotnet publish -c Release -o ./publish`
  - Upload artifact: `actions/upload-artifact@v4` (./publish)

- **Docker job** (on tag or main):
  - Checkout; set up Docker Buildx
  - Build image from Dockerfile; tag with git tag or sha
  - Push to GHCR (or other registry) with `docker/login-action` and `docker/build-push-action`

### 6.3 Secrets and configuration in CI

- **Secrets:** Store in GitHub Secrets (or equivalent): no connection strings for production in repo. Use secrets for registry login and, if needed, staging DB URL.
- **Test DB:** Use Testcontainers (no secret) or a CI Postgres service with a non-sensitive test URL (e.g. `postgres://postgres:postgres@localhost:5432/test` in CI env).

---

## 7. Staging and production deployment

### 7.1 Staging

- **Goal:** Validate full stack and E2E before production.
- **Deploy:** Run Docker Compose or Aspire with staging config; or deploy to a staging host/container platform.
- **Config:** Staging-specific env: `DATABASE_URL` (or connection string) pointing to staging Postgres; `PROJECT_MCP_ROOT` if needed; logging sink (e.g. Seq) for staging.
- **Data:** Apply migrations; optionally seed minimal data for E2E. No production data.
- **Verification:** Run E2E suite against staging endpoint; run [manual testing checklist](12-testing-plan.html#61-manual-test-checklist).

### 7.2 Production

- **Goal:** Reliable, secure service for end users.
- **Deploy:** Same container image as tested in staging; only configuration and secrets differ.
- **Config (env):**
  - `ConnectionStrings__DefaultConnection` or `DATABASE_URL` — production Postgres (from secret store).
  - `PROJECT_MCP_ROOT` — optional; must be a safe path on the host/volume if used.
  - `PROJECT_MCP_ENTERPRISE_ID` / `PROJECT_MCP_PROJECT_ID` — optional default scope.
  - Serilog / logging: configurable sink URL (e.g. from env); no secrets in logs.
- **Secrets:** Injected at deploy time (e.g. Kubernetes secrets, Docker secrets, or env from vault). Never in image or repo.
- **Migrations:** Run migrations as a separate step or job before starting the new server version (e.g. CI job “migrate” or run in init container). Ensure backward compatibility so old server can still run until new one is up.

### 7.3 Health and readiness

- **Liveness:** Process runs and responds to stdio (no separate HTTP health for v1). Orchestrator can use “process running” as liveness.
- **Readiness:** If possible, expose a minimal “ready” check (e.g. DB ping). For stdio-only, readiness may be “process started and DB connection succeeded once at startup.”
- **Startup:** Server should fail fast if DB is unreachable or connection string is missing; log clearly.

---

## 8. Configuration reference

| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` or `DATABASE_URL` or `PROJECT_MCP_CONNECTION_STRING` | Yes | PostgreSQL connection string | `Host=localhost;Database=projectmcp;Username=...;Password=...` |
| `PROJECT_MCP_ROOT` | No | Optional project root; default = cwd | `/repo` |
| `PROJECT_MCP_ENTERPRISE_ID` | No | Default enterprise for scope | GUID or slug |
| `PROJECT_MCP_PROJECT_ID` | No | Default project for scope | GUID or slug |
| Serilog / logging sink | No | Configurable logging (e.g. `SERILOG__WRITE_TO`, sink URL) | Seq, Application Insights, etc. |

---

## 9. Rollback plan

| Step | Action |
|------|--------|
| 1 | Identify last known good version (tag or image digest). |
| 2 | Stop or scale down current deployment. |
| 3 | Redeploy previous image or artifact; restore previous config/secrets. |
| 4 | If DB migrations were applied and are not backward-compatible, run a separate rollback migration (prepare these in advance for breaking schema changes). |
| 5 | Run smoke tests (e.g. scope_set, project_get_info, task_list). |
| 6 | Notify and document incident; post-mortem if needed. |

- **Recommendation:** Keep migrations additive where possible so that rolling back the app does not require a DB rollback.

---

## 10. Operational checklist

### 10.1 Pre-deployment

- [ ] All tests pass in CI (unit, integration, optional E2E).
- [ ] No secrets in code or in image layers.
- [ ] Migration scripts tested on a copy of staging/prod schema (if applicable).
- [ ] Release notes or changelog updated; version tagged.

### 10.2 Post-deployment

- [ ] Server process is running (or container healthy).
- [ ] Can connect to Postgres from the server (e.g. run one tool that hits DB).
- [ ] Logs are flowing to configured sink; no secrets in logs.
- [ ] Manual smoke: scope_set, project_get_info, task_list (or equivalent) succeed from Cursor or test client.

### 10.3 Monitoring (future)

- Log aggregation and search (e.g. by correlation_id, context_key).
- Optional: metrics (request count, latency per tool) if the server exposes them later.
- Alerts on repeated errors or DB connection failures.

---

## 11. Summary

- **Local:** Postgres + env vars + `dotnet run` (or Aspire for full stack); Cursor attaches to server stdio.
- **Docker:** Dockerfile for server; Compose for server + Postgres; connection string and optional env passed at run time.
- **CI/CD:** Build, test (unit + integration), publish artifact, build and push Docker image; deploy staging then production with env and secrets.
- **Rollback:** Redeploy previous image and config; avoid breaking DB migrations when possible.
- **Operations:** Pre/post checklists; config reference; no secrets in repo or logs.

This plan gives a detailed path from local development through to production deployment and rollback, consistent with the existing deployment and tech requirements docs.
