---
title: Tech and Implementation
---

# Tech Choices and Implementation Order

## Tech choices

- **Implementation:** .NET (C#). Use the official MCP .NET SDK for the MCP server (stdio transport) and **ASP.NET Core** (or equivalent) to expose **REST endpoints** for the same tools and resources. Stdio for IDE (e.g. Cursor); REST for scripts, CI, and remote clients.
- **TODO engine library:** Encapsulate the TODO/work-item domain logic in a reusable library (e.g. `ProjectMcp.TodoEngine`). The library provides DI extension methods for setup and assumes services and data contexts are already registered in `IServiceProvider`. It follows the Go4 MVC pattern using `GPS.SimpleMvc`; all TODO API interaction goes through a View interface that extends `IView`. It uses EF Core with configurable providers for PostgreSQL, SQLite, and SQL Server.
- **Storage:** PostgreSQL for v1 deployments; provider selection is configurable via EF Core. Connection string via env (e.g. `DATABASE_URL` or `PROJECT_MCP_CONNECTION_STRING`).
- **Discovery:** Server started by the IDE (e.g. Cursor) via command for stdio; when run as an HTTP host, the server listens on a configurable port (e.g. env or `ASPNETCORE_URLS`).

**Note:** **Work items and tasks are the same entity**; a **task** is a **work item** with **level = Task**.

## Implementation order (when moving to code)

1. **Scaffold** — .NET project that supports both stdio (MCP .NET SDK) and HTTP (e.g. ASP.NET Core); add MCP .NET SDK, single tool (e.g. `project_get_info`) to verify server runs over stdio and REST.
2. **TODO engine library** — Create the reusable TODO engine library with DI extension methods (e.g. `AddTodoEngine`) that resolve services/data contexts from `IServiceProvider`, implement the Go4 MVC structure using `GPS.SimpleMvc`, and route all TODO API interaction through a View interface extending `IView`.
3. **Data layer** — Model types for Project, **WorkItem (level = Work | Task)**, Milestone, Release; PostgreSQL schema (tables/migrations); data access with EF Core and provider packages for PostgreSQL, SQLite, and SQL Server.
4. **Resources** — Implement `project://current/spec`, `project://current/tasks`, `project://current/plan` by reading from the data layer.
5. **Project tools** — `project_update`, `project_get_info` (if not already), tech stack updates.
6. **Task tools** — `task_create`, `task_update`, `task_list`, `task_delete` (**work items with level = Task**).
7. **Planning tools** — `milestone_*`, `release_*`.
8. **Docs and config** — README (how to add server to Cursor, env vars, database setup), migration/schema docs or example seed data.

## Future: integrations

- **Adapter interface** — e.g. `IProjectStore` with `GetProject()`, `GetTasks()`, `SaveTask()`, etc. (**tasks are work items with level = Task**). Primary implementation: PostgreSQL (e.g. `PostgresProjectStore` or EF Core).
- **GitHub** — `GitHubProjectStore`: tasks → Issues, milestones → GitHub Milestones, optional Project board; auth via `GITHUB_TOKEN` and repo config.
- **Config** — Env or config to choose backend (e.g. `PROJECT_MCP_BACKEND=local|github`) and repo/owner for GitHub.
