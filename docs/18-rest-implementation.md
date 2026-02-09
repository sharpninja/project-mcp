---
title: REST Service — Implementation Plan
---

# REST Service — Excruciatingly Detailed Implementation Plan

This document provides an **excruciatingly detailed** implementation plan for the MCP REST service layer. It complements [11 — Implementation Plan](11-implementation-plan.html) Phase 2b and [04 — MCP Surface](04-mcp-surface.html). The REST layer exposes the same tools and resources as the stdio MCP server over HTTP so that scripts, CI, and remote clients can invoke them using **context_key** and optional **correlation_id** in headers.

**References:** [02 — Architecture](02-architecture.html), [04 — MCP Surface](04-mcp-surface.html), [06 — Tech Requirements](06-tech-requirements.html), [11 — Implementation Plan](11-implementation-plan.html).

---

## 1. Scope and goals

### 1.1 In scope

- HTTP host (ASP.NET Core) running alongside or instead of stdio.
- REST base path (e.g. `/mcp`) with **initialize**, **tool call**, and **resource** endpoints.
- **Context key** issued at initialize; required on every subsequent request via header.
- **Correlation ID** optional header; propagated to logs and audit.
- Same session store, scope semantics, and tool/resource handlers as stdio.
- Request/response JSON; consistent error shape; no secrets in responses.

### 1.2 Out of scope (v1)

- MCP-over-SSE or streaming HTTP.
- OpenAPI/Swagger UI (can be added later).
- API versioning in URL (e.g. `/v1/mcp`); single version for v1.

---

## 2. Project and hosting

### 2.1 Project layout

| Item | Detail |
|------|--------|
| **Host project** | The MCP server solution has one host that can run in **stdio-only**, **HTTP-only**, or **dual** mode. HTTP is provided by ASP.NET Core (Kestrel). |
| **Entry point** | `Program.cs` (or host builder) checks env (e.g. `PROJECT_MCP_HTTP_ENABLED=true` or `ASPNETCORE_URLS` set) to enable Kestrel. If only stdio is needed (e.g. Cursor), HTTP can be disabled. |
| **Port** | Configurable via `ASPNETCORE_URLS` (e.g. `http://localhost:5000`) or `PROJECT_MCP_HTTP_PORT` (e.g. `5000`) that is translated to `Urls`. No default port in code; require explicit config when HTTP is enabled. |

### 2.2 Kestrel and URL configuration

- **Step 2.2.1:** In the host project, add package reference `Microsoft.AspNetCore.App` (or ensure SDK is `Microsoft.NET.Sdk.Web`) so Kestrel is available.
- **Step 2.2.2:** Read `ASPNETCORE_URLS` from environment. If present and non-empty, use it as the URL list for Kestrel.
- **Step 2.2.3:** If `ASPNETCORE_URLS` is not set but `PROJECT_MCP_HTTP_PORT` is set (e.g. `5000`), set `Urls` to `http://*:{port}` (or `http://localhost:{port}` for dev). Document that binding to `*` may require run-as permissions on Linux.
- **Step 2.2.4:** If HTTP is enabled (URLs configured), call `WebApplication.CreateBuilder(args)` (or add `UseKestrel()` and map endpoints) so the app listens on the configured addresses. Ensure the same `IHost` or `WebApplication` registers the MCP services (session store, scope, tool handlers) so both stdio and HTTP share the same service provider where applicable.
- **Step 2.2.5:** Do **not** enable HTTPS in code by default; document that in production a reverse proxy (e.g. nginx, YARP) should terminate TLS and forward to the app.

### 2.3 Middleware order (HTTP pipeline)

Document and implement the following order (top = first):

1. **Exception handling** — Catch unhandled exceptions; return 500 with a generic message; log with full detail (no secrets). Optionally use `UseExceptionHandler` with a custom handler that sets correlation_id and context_key from headers on the log context.
2. **Request logging** — Optional middleware that logs request path, method, and (if present) correlation_id and context_key (last 4 chars only in logs to avoid leaking full key). Do not log request body (may contain PII).
3. **Correlation ID extraction** — Read `X-Correlation-Id` or `MCP-Correlation-Id`; set in `HttpContext.Items["CorrelationId"]` and in a scoped service or `AsyncLocal` so all downstream code can access it. Do not validate value; pass through.
4. **Context key validation** — For all routes under `/mcp` **except** `POST /mcp/initialize`, require header `X-Context-Key` or `MCP-Context-Key`. If missing or invalid (unknown/expired session), return **401 Unauthorized** with body `{ "error": "Missing or invalid context key." }` (or equivalent). Do not run tool or resource handler.
5. **Session scope resolution** — Resolve context_key to session; load scope (enterprise_id, project_id) and resource_id into HttpContext.Items or a scoped `IMcpRequestContext` so handlers do not need to look up again.
6. **Endpoint routing** — Map routes to handlers (see §3 and §4).

---

## 3. REST base path and route table

### 3.1 Base path

- **Base path:** `/mcp`. All MCP REST routes are prefixed with `/mcp`.
- **Documentation:** Document in README and API docs that the base URL is e.g. `http://localhost:5000/mcp` and that clients must send `Content-Type: application/json` and accept `application/json` for JSON request/response.

### 3.2 Route table (exact list)

| Method | Route | Description | Context key required |
|--------|-------|-------------|------------------------|
| POST   | `/mcp/initialize` | Initialize session; returns context_key, capabilities, tools, resources | No |
| POST   | `/mcp/tools/call` | Invoke a tool by name with arguments | Yes |
| GET    | `/mcp/resources/{*path}` | Read a resource by path (e.g. `project/current/spec`) | Yes |

**Rationale for `/mcp/tools/call`:** A single endpoint with body `{ "name": "toolName", "arguments": { ... } }` keeps the surface small and matches MCP tool-invocation semantics. Alternative (e.g. `POST /mcp/tools/{toolName}`) can be added later if needed.

### 3.3 Route implementation (minimal APIs or controllers)

- **Option A (recommended for v1):** Use **minimal APIs** in the host project (e.g. `app.MapPost("/mcp/initialize", ...)`). Group under a single `RouteGroupBuilder` with prefix `/mcp` so all routes share the same prefix and middleware.
- **Option B:** Use a dedicated **API controller** (e.g. `McpRestController`) with `[Route("mcp")]` and actions for Initialize, CallTool, GetResource. Inject `ISessionStore`, `IToolDispatcher`, `IResourceResolver` (or equivalent) into the controller.

**Step 3.3.1:** Create a class or extension method that maps the three routes and registers the handlers. Ensure the **context key validation** middleware runs only for `/mcp/tools/call` and `/mcp/resources/*`, not for `/mcp/initialize`.

---

## 4. Initialize endpoint

### 4.1 Contract

- **Request:** `POST /mcp/initialize`
- **Request headers:** `Content-Type: application/json`. No context key.
- **Request body (JSON):**

```json
{
  "protocolVersion": "2024-11-05",
  "capabilities": {},
  "clientInfo": {
    "name": "cursor",
    "version": "0.1.0"
  }
}
```

- **Response:** `200 OK`, `Content-Type: application/json`
- **Response body (success):**

```json
{
  "protocolVersion": "2024-11-05",
  "capabilities": {
    "tools": {},
    "resources": {}
  },
  "serverInfo": {
    "name": "project-mcp",
    "version": "1.0.0"
  },
  "contextKey": "550e8400-e29b-41d4-a716-446655440000",
  "tools": [ { "name": "scope_set", "description": "...", "inputSchema": { ... } }, ... ],
  "resources": [ { "uri": "project://current/spec", ... }, ... ]
}
```

- **Response (agent not approved):** `401 Unauthorized` with body e.g. `{ "error": "Unauthorized. Agent not approved for Enterprise." }` when the client name does not resolve to a Resource in the default (or configured) enterprise.

### 4.2 Implementation steps

- **Step 4.2.1:** Define a C# DTO for the initialize request: `McpInitializeRequest` with `ProtocolVersion`, `Capabilities`, `ClientInfo` (Name, Version). Deserialize from body.
- **Step 4.2.2:** Define a response DTO: `McpInitializeResponse` with `ProtocolVersion`, `Capabilities`, `ServerInfo`, **ContextKey**, `Tools` (list of tool descriptors), `Resources` (list of resource URI templates). Serialize to JSON with camelCase.
- **Step 4.2.3:** In the initialize handler: (1) Read `clientInfo.name` (e.g. "cursor"). (2) Resolve the name to a **Resource** in the enterprise (use default enterprise from config if no scope yet). If no resource found, return 401 with the "Agent not approved" message and do not create a session. (3) Create a new session: generate a new GUID (or signed token) as **context_key**, create session entry with scope = default from config (or empty), store **resource_id** (resolved agent) in the session. (4) Register session in the session store keyed by context_key. (5) Build the list of tools (names, descriptions, input schemas) and resources (URIs) from the same registry used by stdio. (6) Return 200 with response body including **context_key**, tools, and resources.
- **Step 4.2.4:** Ensure the session store is the same implementation used by stdio (e.g. in-memory dictionary keyed by context_key; TTL optional for v1). Document that REST and stdio do not share sessions (different transport); each REST initialize creates a new session.

---

## 5. Tool call endpoint

### 5.1 Contract

- **Request:** `POST /mcp/tools/call`
- **Request headers:** `Content-Type: application/json`, **X-Context-Key** or **MCP-Context-Key** (required), **X-Correlation-Id** or **MCP-Correlation-Id** (optional).
- **Request body (JSON):**

```json
{
  "name": "scope_set",
  "arguments": {
    "scope_slug": "E1-P001"
  }
}
```

- **Response (success):** `200 OK`, body = tool result content (e.g. JSON text in MCP format or direct JSON object).
- **Response (validation error):** `400 Bad Request`, body e.g. `{ "error": "scope_slug is required.", "isError": true }`.
- **Response (scope/not found):** `404 Not Found` or `403 Forbidden` when the tool succeeds but the operation finds no data or cross-enterprise; prefer a structured body `{ "error": "...", "isError": true }` and 200 if the MCP convention is to return errors in content; otherwise 403/404. **Decision:** For v1, use **200** with `{ "error": "...", "isError": true }` in body for tool-level errors (validation, not found, cross-enterprise); use **401** only for invalid/missing context key; use **500** for unhandled exceptions.
- **Response (401):** Missing or invalid context key (handled by middleware).

### 5.2 Implementation steps

- **Step 5.2.1:** Define request DTO: `ToolCallRequest` with `Name` (string), `Arguments` (JsonElement or Dictionary<string, object>). Deserialize from body.
- **Step 5.2.2:** Handler logic: (1) Middleware has already validated context_key and resolved session; session scope and resource_id are in context. (2) Look up the tool by `Name` in the same tool registry used by stdio. If not found, return 400 with `{ "error": "Unknown tool: {name}.", "isError": true }`. (3) Invoke the **same** tool handler (delegate or service) that stdio uses, passing `Arguments` and the session scope (enterprise_id, project_id) and resource_id. (4) Tool handler returns a result object or throws. (5) If result indicates error (e.g. `isError: true`), return 200 with that result body. (6) Otherwise return 200 with the tool result as JSON body.
- **Step 5.2.3:** Ensure **correlation_id** from header is passed into the tool execution context (e.g. scoped service or AsyncLocal) so that repository/audit code can attach it to change records and logs.
- **Step 5.2.4:** Ensure **enterprise scope** is enforced inside each tool implementation (not only in REST layer): any access to another enterprise must return an error and log the attempt with context_key, tool name, requested ids, target enterprise.

---

## 6. Resource endpoint

### 6.1 Contract

- **Request:** `GET /mcp/resources/{*path}`  
  Example: `GET /mcp/resources/project/current/spec`, `GET /mcp/resources/project/current/tasks`, `GET /mcp/resources/enterprise/current/resources`, `GET /mcp/resources/work_item/{id}`.
- **Request headers:** **X-Context-Key** or **MCP-Context-Key** (required), **X-Correlation-Id** or **MCP-Correlation-Id** (optional).
- **Response (success):** `200 OK`, `Content-Type: application/json`, body = resource content (e.g. project spec JSON, task list, plan, work item detail).
- **Response (not found / invalid scope):** `404 Not Found` with body `{ "error": "Resource not found or out of scope." }` (or 200 with isError in body per project convention).
- **Response (401):** Missing or invalid context key.

### 6.2 Path mapping

Map URL path to MCP resource URI:

- `/mcp/resources/project/current/spec` → `project://current/spec`
- `/mcp/resources/project/current/tasks` → `project://current/tasks`
- `/mcp/resources/project/current/plan` → `project://current/plan`
- `/mcp/resources/project/current/requirements` → `project://current/requirements`
- `/mcp/resources/project/current/issues` → `project://current/issues`
- `/mcp/resources/enterprise/current/resources` → `enterprise://current/resources`
- `/mcp/resources/work_item/{id}` → `work_item://{id}` (id = GUID or slug)

**Step 6.2.1:** Implement a path parser that converts the remaining path after `/mcp/resources/` into the internal resource URI form (e.g. "project/current/spec" → "project://current/spec"). Use the same **resource resolver** that stdio uses: given URI and session scope, load the resource content and return it. If the resource is out of scope or not found, return 404 (or 200 with error body).

---

## 7. Headers (exact names and behavior)

| Header | Purpose | Required | Validation |
|--------|---------|----------|------------|
| **X-Context-Key** or **MCP-Context-Key** | Session identifier; issued at initialize | Yes (except on initialize) | Must resolve to a valid, non-expired session; otherwise 401. |
| **X-Correlation-Id** or **MCP-Correlation-Id** | Request correlation | No | No validation; pass through to logs and audit. |
| **Content-Type** | Request body type | Yes (for POST with body) | Expect `application/json` for initialize and tools/call; return 415 if wrong. |
| **Accept** | Response preference | No | If present, prefer `application/json`; response is always JSON for v1. |

**Step 7.1:** In middleware, check both header names for context key and correlation id (e.g. prefer `MCP-Context-Key`, fallback to `X-Context-Key`). Document in API docs that clients may send either.

---

## 8. Error responses (consistent shape)

All JSON error responses must use a consistent shape so clients can parse them uniformly:

```json
{
  "error": "Human-readable message.",
  "isError": true
}
```

- **401 Unauthorized:** Missing or invalid context key. Body: `{ "error": "Missing or invalid context key.", "isError": true }`.
- **400 Bad Request:** Invalid request body or unknown tool. Body: `{ "error": "...", "isError": true }`.
- **404 Not Found:** Resource not found or out of scope. Body: `{ "error": "Resource not found or out of scope.", "isError": true }`.
- **500 Internal Server Error:** Unhandled exception. Body: `{ "error": "An internal error occurred.", "isError": true }`. Do not leak exception message or stack trace in response; log full detail server-side.

---

## 9. Logging and correlation

- **Step 9.1:** For every REST request, attach to the log context: **CorrelationId** (from header), **ContextKey** (last 4 chars or hashed if full key is sensitive), **Path**, **Method**. Use Serilog's `ForContext` or similar so that all logs emitted during that request include these properties.
- **Step 9.2:** When calling the tool or resource layer, pass correlation_id and context_key (or session id) so that audit and change tracking can record them. See [06 — Tech Requirements](06-tech-requirements.html) (Logging).

---

## 10. CORS (optional for v1)

- **Step 10.1:** If the REST API will be called from a browser (e.g. a future Blazor WebAssembly app calling the MCP REST API), configure CORS: allow specific origins from config (e.g. `PROJECT_MCP_CORS_ORIGINS`). For v1, CORS can be disabled if only server-side or script clients are used; document how to enable and set allowed origins.

---

## 11. Task checklist (summary)

| ID | Task | Dependency |
|----|------|------------|
| R.1 | Add ASP.NET Core/Kestrel to host; configurable URLs from env | Phase 0 |
| R.2 | Implement middleware: correlation ID extraction, context key validation (skip for initialize), session scope resolution | R.1 |
| R.3 | Define DTOs: McpInitializeRequest/Response, ToolCallRequest; error body shape | — |
| R.4 | Implement POST /mcp/initialize: resolve agent to Resource, create session, return context_key and tool/resource lists | R.2, session store |
| R.5 | Implement POST /mcp/tools/call: dispatch to same tool handlers as stdio; pass scope and correlation_id | R.2, tool registry |
| R.6 | Implement GET /mcp/resources/{*path}: map path to resource URI, call same resource resolver as stdio | R.2, resource resolver |
| R.7 | Document base URL, headers (X-Context-Key, MCP-Context-Key, X-Correlation-Id), initialize flow, and example requests in README or API docs | R.4–R.6 |
| R.8 | Add unit or integration tests: initialize returns context_key; tools/call with valid key invokes tool; missing key returns 401 | R.4–R.6 |

---

## 12. Dependencies on other phases

- **Session store and scope (Phase 2):** REST depends on the same session store, scope_set/scope_get behavior, and default scope from config.
- **Tool and resource handlers (Phases 3–6, 9):** REST only routes to existing handlers; no duplicate business logic.
- **Logging (Phase 7):** Correlation ID and context key in logs; exception logging with Data and inner exceptions per [06 — Tech Requirements](06-tech-requirements.html).

This plan is the single reference for implementing the REST service layer in excruciating detail.
