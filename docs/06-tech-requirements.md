---
title: Tech Requirements
---

# Tech Requirements

Technical requirements for the Software Project Management MCP server. **Methodology neutrality:** The design does not assign or assume any methodology from outside (e.g. agile, Waterfall); the model is methodology-agnostic.

## Runtime

- **.NET** — Target .NET 8 or later (LTS). Required for MCP SDK and supported tooling.
- **Execution** — Server runs as a single process. Launched by the host (e.g. Cursor) via command line (e.g. `dotnet run` or path to published executable); stdio transport for MCP.

## Protocol and SDK

- **MCP** — Model Context Protocol. Server implements the standard MCP server surface.
- **Transport** — stdio (required for typical IDE integration). HTTP/SSE or other transports are out of scope for v1.
- **SDK** — Official MCP .NET SDK (e.g. `ModelContextProtocol` or equivalent server package). No custom protocol handling.

## Language and build

- **Language** — C#. Nullable reference types and modern C# encouraged.
- **Project type** — Console application or worker suitable for stdio. Standard SDK-style project (e.g. `dotnet new console`).
- **Output** — Build with `dotnet build`; run with `dotnet run` or publish (e.g. `dotnet publish -c Release`) for a self-contained or framework-dependent executable.
- **Dependencies** — MCP .NET SDK, .NET BCL, and a PostgreSQL driver or ORM (e.g. Npgsql, EF Core with Npgsql). No optional backends until the integration phase.

## Storage

- **Backend** — PostgreSQL. Required for v1.
- **Connection** — Configured via environment (e.g. `DATABASE_URL`, `PROJECT_MCP_CONNECTION_STRING`, or `ConnectionStrings__DefaultConnection`). No hardcoded connection strings.
- **Schema** — Tables for project (metadata, tech stack, docs), tasks, milestones, releases. UTF-8 encoding. Use standard PostgreSQL types; JSONB acceptable for tech stack, labels, or doc list if preferred.
- **Scope** — Single logical project per server process (or per connection) in v1; optional project_id on tables for future multi-project support.
- **Project root** — Optional env (e.g. `PROJECT_MCP_ROOT`) or process cwd. Used only for **doc_read** (file paths in the repo); not for storing project data.

## Environment and configuration

- **Project root** — Optional env var (e.g. `PROJECT_MCP_ROOT`) for **doc_read** file resolution. If unset, use process cwd.
- **Database connection** — Required env or config for PostgreSQL (e.g. `DATABASE_URL`). No secrets in repo; use env or secret store in deployment.
- **No secrets in repo** — No API keys or tokens in design or default config. Future integrations (e.g. GitHub) will use env-provided tokens.
- **No config file required** — Server must run with no config file; env and MCP tool arguments are sufficient for v1.

## Security and safety

- **Path safety** — Any tool that reads files by path (e.g. `doc_read`) must resolve paths relative to project root and reject paths that escape the root (no `..` or absolute paths outside root).
- **Write scope** — Database writes only to the configured PostgreSQL database. File writes (if any) only under the designated project directory. No writes to arbitrary system paths.
- **Input validation** — All tool arguments must be validated (types, allowed values). Invalid input must return a clear error, not crash.

## Deployment

- **Containers** — The MCP server and PostgreSQL are deployed as Docker containers.
- **Orchestration** — .NET Aspire: an App Host project composes the application, starts the Postgres container and the MCP server (as a container or process), and injects connection strings and configuration. See [07 — Deployment](07-deployment.md).
- **Dockerfile** — The repo includes a Dockerfile (or Dockerfile context) for the MCP server; multi-stage build, runtime image. Postgres is run via Aspire (e.g. AddPostgresContainer) or an official Postgres image.
- **No VM or bare-metal** — Design assumes containerized deployment; no explicit requirement for non-container hosting.

## Compatibility

- **MCP spec** — Implement a version of the MCP specification supported by the official SDK (current at implementation time).
- **Hosts** — Designed for use with MCP clients such as Cursor; no host-specific code required beyond stdio and standard MCP requests/responses.

## Non-requirements (v1)

- No other databases (e.g. SQLite) as primary store; PostgreSQL only.
- No HTTP server or SSE in process.
- No authentication or authorization (trust the host process).
- No multi-project or multi-workspace support in a single process.
- No telemetry or external network calls except for future integration backends.
