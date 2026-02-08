---
title: Tech and Implementation
---

# Tech Choices and Implementation Order

## Tech choices

- **Implementation:** .NET (C#). Use the official MCP .NET SDK for the MCP server (stdio transport; HTTP/SSE can be added later if needed).
- **Storage:** PostgreSQL. Connection string via env (e.g. `DATABASE_URL` or `PROJECT_MCP_CONNECTION_STRING`). Optional env: `PROJECT_MCP_ROOT` for project root (used for **doc_read** file resolution).
- **Discovery:** Server started by the IDE (e.g. Cursor) via command (e.g. `dotnet run` or path to published executable).

## Implementation order (when moving to code)

1. **Scaffold** — .NET project (e.g. console app or worker), add MCP .NET SDK, single tool (e.g. `project_get_info`) to verify server runs.
2. **Data layer** — Model types for Project, Task, Milestone, Release; PostgreSQL schema (tables/migrations); data access (e.g. EF Core, Dapper, or ADO); resolve project root from env or current directory for **doc_read**.
3. **Resources** — Implement `project://current/spec`, `project://current/tasks`, `project://current/plan` by reading from the data layer.
4. **Project tools** — `project_update`, `project_get_info` (if not already), tech stack and doc list updates.
5. **Task tools** — `task_create`, `task_update`, `task_list`, `task_delete`.
6. **Planning tools** — `milestone_*`, `release_*`.
7. **Doc tools** — `doc_register`, `doc_list`; optionally `doc_read`.
8. **Docs and config** — README (how to add server to Cursor, env vars, database setup), migration/schema docs or example seed data.

## Future: integrations

- **Adapter interface** — e.g. `IProjectStore` with `GetProject()`, `GetTasks()`, `SaveTask()`, etc. Primary implementation: PostgreSQL (e.g. `PostgresProjectStore` or EF Core).
- **GitHub** — `GitHubProjectStore`: tasks → Issues, milestones → GitHub Milestones, optional Project board; auth via `GITHUB_TOKEN` and repo config.
- **Config** — Env or config to choose backend (e.g. `PROJECT_MCP_BACKEND=local|github`) and repo/owner for GitHub.
