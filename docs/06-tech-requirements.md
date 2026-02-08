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
- **Schema** — Tables for project (metadata, tech stack), work_items (**level = Work | Task; tasks are level = Task**), milestones, releases. UTF-8 encoding. Use standard PostgreSQL types; JSONB acceptable for tech stack, labels where specified.
- **Change tracking** — **Change tracking must be enabled on all fields of all entities** in the database. Every insert, update, and delete to entity data must be recorded (e.g. via audit tables, triggers, or application-level logging) so that what changed, when, and optionally by whom can be determined. Each change record must include the **session identifier**, **resource identifier**, and **correlation id** of the request (when available). No entity or field may be excluded. See [03 — Data Model](03-data-model.html) (Change tracking).
- **Scope** — Single logical project per server process (or per connection) in v1; optional project_id on tables for future multi-project support.

## Environment and configuration

- **Database connection** — Required env or config for PostgreSQL (e.g. `DATABASE_URL`). No secrets in repo; use env or secret store in deployment.
- **No secrets in repo** — No API keys or tokens in design or default config. Future integrations (e.g. GitHub) will use env-provided tokens.
- **No config file required** — Server must run with no config file; env and MCP tool arguments are sufficient for v1.

## Logging

- **Structured logging** — The service uses **Serilog** for structured logging. **All structured logging records must include, when available:** **session identification** (e.g. MCP context_key or web/API session id), **resource identification** (e.g. resource_id of the authenticated user or agent), and **correlation id** (when the client sends one). Additional properties (e.g. tool name, scope, endpoint) may be included. Log events are structured so that logs can be queried and filtered by session, resource, and correlation id.
- **Logged exceptions** — When an exception is logged, the **name-value pairs in the exception’s `Data` property** (e.g. `Exception.Data`) must be added as **structured properties of the log entry**. This allows any context attached to the exception (e.g. entity id, operation, request id) to be queried and filtered in the logging provider. The logging layer (e.g. Serilog enricher or exception destructurer) should enumerate `exception.Data` and add each key-value pair as a structured field on the log event; keys that are not safe for the log sink (e.g. non-scalar or secret values) should be sanitized or omitted.
- **Inner exceptions** — Logged exceptions that have an **inner exception** must produce **another log entry** for the inner exception, and repeat **recursively** for each inner exception in the chain. Each entry should include the exception type, message, stack trace, and (per above) the exception’s `Data` properties as structured fields, and should reference the parent (e.g. `InnerExceptionIndex` or similar) so the full chain can be reconstructed. This gives operators one log event per exception in the chain for easier querying and correlation.
- **Configurable logging provider** — Logs are pushed to a **configurable logging provider service** (e.g. Seq, Application Insights, Elasticsearch, or another sink). The target is not hardcoded; it is configured via environment or configuration (e.g. `SERILOG__WRITE_TO`, connection URL for the provider, or standard Serilog sink configuration). The server supports at least one configurable sink so that deployments can send logs to the operator’s chosen provider.
- **Non-blocking async writes** — Writes to the logging service must be **non-blocking async calls**. The application workflow must not wait on the logging provider’s completion; logging must not add latency to request handling or tool execution.
- **Local buffer and queue** — Use a **local buffer** to **queue log submissions** so that workflow is **decoupled from logging**. The application enqueues log events to an in-process buffer (e.g. a bounded queue or Serilog async sink); a background writer or sink drains the buffer and sends to the logging provider asynchronously. This keeps the hot path non-blocking and isolates the workflow from provider slowness or transient failures.
- **No secrets in logs** — Logging must not emit secrets (connection strings, tokens, or other sensitive data). Structured properties should be safe for the configured provider.
- **Retention** — Recommended retention: **90 days** for routine logs and **1 year** for cross-enterprise access attempts (or per enterprise policy if stricter).

## Security and safety

- **Enterprise scope on every endpoint** — Every endpoint (MCP tools and resources, web app and API) must **check that any data requested or submitted is within the enterprise(s) the user or agent is associated with**. For reads: return only data whose enterprise_id (or project’s enterprise_id) is in the session’s or user’s allowed enterprises. For writes: reject requests that would create or modify data in another enterprise. Do not rely on the client to restrict scope; enforce in the backend on every call.
- **MCP agent identity** — The MCP client name (e.g. “cursor”, “copilot”) **must resolve to a Resource** in the enterprise. If no matching Resource exists, return **Unauthorized. Agent not approved for Enterprise** and reject the request; the resolved resource_id is used for audit logging.
- **Cross-enterprise access attempts: log and follow up** — Any attempt to access data belonging to **another enterprise** (e.g. requesting a project by id that belongs to an enterprise not in the user’s scope, or submitting an update for an entity in another enterprise) must be **logged** with sufficient detail (e.g. endpoint, user/agent identity, session or context_key, requested entity or ids, target enterprise) for manual follow-up. Return an error (e.g. 403 Forbidden or equivalent) to the caller; do not return the data. Logging must be structured so operators can query for cross-enterprise attempts and follow up manually (e.g. security review, abuse investigation).
- **Audit history access** — Provide a **read-only audit/history API** for SUDO/admin users to query by entity, date range, session, or resource.
- **Path safety** — Any tool that accepts file paths must resolve them relative to a configured root and reject path traversal (no `..` or absolute paths outside root).
- **Write scope** — Database writes only to the configured PostgreSQL database. File writes (if any) only under the designated project directory. No writes to arbitrary system paths.
- **Input validation** — All tool arguments must be validated (types, allowed values). Invalid input must return a clear error, not crash.

## Deployment

- **Containers** — The MCP server and PostgreSQL are deployed as Docker containers.
- **Orchestration** — .NET Aspire: an App Host project composes the application, starts the Postgres container and the MCP server (as a container or process), and injects connection strings and configuration. See [07 — Deployment](07-deployment.html).
- **Dockerfile** — The repo includes a Dockerfile (or Dockerfile context) for the MCP server; multi-stage build, runtime image. Postgres is run via Aspire (e.g. AddPostgresContainer) or an official Postgres image.
- **No VM or bare-metal** — Design assumes containerized deployment; no explicit requirement for non-container hosting.

## Compatibility

- **MCP spec** — Implement a version of the MCP specification supported by the official SDK (current at implementation time).
- **Hosts** — Designed for use with MCP clients such as Cursor; no host-specific code required beyond stdio and standard MCP requests/responses.

## Non-requirements (v1)

- No other databases (e.g. SQLite) as primary store; PostgreSQL only.
- No HTTP server or SSE in process.
- No end-user authentication for MCP tools beyond **agent identity** (agent name must resolve to a Resource).
- No multi-project or multi-workspace support in a single process.
- No telemetry or external network calls except for future integration backends.
