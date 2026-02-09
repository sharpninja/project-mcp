---
title: MCP Surface
---

# MCP Surface

## Context key

**The server issues a context key as part of the initial handshake. The agent must include this context key in all subsequent requests.**

- **Issued at handshake** — When the server responds to the agent’s Initialize request (or immediately after scope is established), it includes a **context_key** in the response. The context key is an opaque value (e.g. a UUID or signed token) that uniquely identifies the session and binds it to the scope and connection state.
- **Included on every request** — The agent **must** send the context key with **every** tool call and resource read/subscribe after the handshake. How it is sent depends on the transport: e.g. a request header (e.g. `X-Context-Key` or `MCP-Context-Key`), a parameter in the envelope, or a field in each tool/resource request. The server **instructs the agent** (via protocol or documentation) to include the context key in all requests.

## REST endpoints

The MCP server **exposes REST endpoints** over HTTP so that tools and resources can be invoked without stdio (e.g. from scripts, CI, or remote clients). The same tool and resource semantics apply; only the transport differs.

- **Base URL** — Configurable (e.g. `http://localhost:5000` or env `ASPNETCORE_URLS`). All MCP REST routes are under a common prefix (e.g. `/mcp` or `/api/mcp`) to avoid clashes.
- **Context key** — REST clients send **context_key** on every request via header (e.g. `X-Context-Key` or `MCP-Context-Key`). Clients obtain a context_key by calling an **initialize** endpoint (e.g. `POST /mcp/initialize` with client name and capabilities); the response includes context_key, server capabilities, tool list, and resource URIs.
- **Correlation ID** — Optional header (e.g. `X-Correlation-Id` or `MCP-Correlation-Id`) on any request; same semantics as stdio.
- **Tools** — Invoked via HTTP (e.g. `POST /mcp/tools/{toolName}` or `POST /mcp/tools/call` with body `{ "name": "toolName", "arguments": { ... } }`). Request body and response are JSON. Scope is taken from the session bound to the context_key.
- **Resources** — Read via HTTP (e.g. `GET /mcp/resources/project/current/spec` or resource path as URL path). Context key in header; response is JSON (or content type per resource).
- **Session** — For REST, the server keys the session by context_key (one session per context_key). Scope is set by calling the **scope_set** tool over REST; thereafter all REST requests with that context_key use that scope until scope_set is called again.
- **Validation** — The server validates the context key on each request. If the key is missing, invalid, or expired, the server returns an error and does not execute the tool or return the resource. Valid keys resolve to the session (and thus the session’s scope); the server uses that session for the operation.
- **Session binding** — The context key ties requests to the session that completed the handshake, so the server can apply the correct scope and reject requests that do not belong to an established session.

## Correlation ID

**The agent may submit a correlation ID with any request to associate related requests.**

- **Optional** — The agent may include a **correlation_id** (e.g. a UUID or opaque string chosen by the agent) on tool calls and resource reads. It is optional; the server does not require it.
- **Related requests** — When the agent sends the **same correlation_id** on multiple requests, the server treats them as related (e.g. one user action that resulted in several tool calls). The server may use the correlation_id for logging, tracing, debugging, or grouping in observability tools. It does not change request semantics or authorization.
- **Transport** — The correlation_id is sent per request (e.g. request header `X-Correlation-Id` or `MCP-Correlation-Id`, or a field in the request envelope). The server passes it through to logs/traces and does not validate or interpret its value.

## Agent identity (resource)

**The agent name must resolve to a Resource in the enterprise.**

- **Agent name required** — The MCP client name (e.g. “cursor”, “copilot”) is used to resolve a **Resource** record in the current enterprise.
- **Unauthorized if missing** — If the agent name does not resolve to a Resource, return **Unauthorized. Agent not approved for Enterprise** and do not execute the tool or return the resource.
- **Audit identity** — The resolved resource_id is recorded on change tracking and logs for MCP-originated requests.

## Scope (enterprise / project)

**The agent sets scope once; the server remembers it for that agent until the agent requests a different scope.**

- **Session scope** — The MCP server associates one **current scope** (enterprise and/or project) with each agent **session** (connection). The server identifies the agent by the connection (e.g. one transport connection = one session). All tools and resources use that session’s current scope; the agent does **not** pass scope on every request.
- **Set scope once** — The agent sets or changes scope by calling the **scope_set** tool (or by sending scope during the initial handshake when the server supports it). After that, every subsequent tool call and resource read uses that scope until the agent calls **scope_set** again with a different enterprise_id and/or project_id.
- **Default scope** — If the agent never sets scope, the server uses a **default scope** from config (e.g. env `PROJECT_MCP_ENTERPRISE_ID`, `PROJECT_MCP_PROJECT_ID`). When no default is configured and the agent has not set scope, the server returns an error indicating scope is required.

**Scope tool:**

- **scope_set** — Set the current session scope. Parameters: **scope_slug** (required; enterprise/project/work item slug), **enterprise_id** (optional, GUID or slug), **project_id** (optional, GUID or slug). The server resolves the slug to the target entity, derives the enterprise/project scope (and child traversal), stores it for the session, and returns the set scope (e.g. `{ enterprise_id, project_id, scope_slug }`). All subsequent requests from this agent use this scope until the agent calls **scope_set** again.
- **scope_get** (optional) — Return the current session scope so the agent can confirm what scope is active without changing it.

Tools and resources do **not** take scope parameters; they always operate in the session’s current scope. This avoids repeating scope on every call and lets the agent switch context only when the user asks to work in a different project or enterprise.

**Enterprise scope enforcement:** Every tool and resource must **verify that any data requested or submitted is within the enterprise (and project, where applicable) the agent is associated with** via the session scope. For example: when loading a project, task, or milestone by id, confirm that the entity’s enterprise_id (or project’s enterprise_id) matches the session’s enterprise; when creating or updating, ensure the target enterprise/project is the session’s. **Any attempt to access or modify data of another enterprise** must be **rejected** (error response), **logged** with sufficient detail (e.g. tool/resource name, context_key, requested ids, target enterprise), and **followed up manually** (e.g. by operators reviewing logs for cross-enterprise access attempts). See [06 — Tech Requirements](06-tech-requirements.html) (Security and safety).

## Resources (read-only)

Expose current project state so the client can load context without calling tools.

| URI | Description |
|-----|-------------|
| `project://current/spec` | Project metadata, tech stack |
| `project://current/tasks` | Full task list |
| `project://current/plan` | Milestones, releases |
| `project://current/requirements` | Requirements list for current project |
| `project://current/issues` | Issues list for current project |
| `enterprise://current/resources` | Resources list for current enterprise |
| `work_item://{id}` | Work item detail and status (subscribe for updates; tasks are work items with level = Task) |

If the MCP SDK does not support query params on resource URIs, use separate resource names for filtered views (e.g. `project://current/tasks/milestone/{id}`) only if needed; v1 can keep a single tasks resource and filter via tools.

## Tools (actions)

Naming: consistent `*_create`, `*_update`, `*_list` (and `*_delete` where applicable).

### Scope

- **scope_set** — Set the current session scope (scope_slug required; enterprise_id/project_id optional). Server remembers this for the agent until the agent calls scope_set again. Returns the set scope.
- **scope_get** — Return the current session scope (read-only; no parameters).

### Enterprise

- **enterprise_create** — Create enterprise (SUDO only): name, description.
- **enterprise_update** — Update enterprise: id, name, description.
- **enterprise_get** — Get enterprise by id or slug.
- **enterprise_list** — List enterprises in scope (SUDO can list all; non-SUDO limited by allowed scope).

### Project

- **project_get_info** — Return current project metadata (name, description, status, tech stack). Return empty/not-initialized if no project exists.
- **project_update** — Create or update project: name, description, status, techStack (object).

### Requirements

- **requirement_create** — title (required), description, acceptanceCriteria, parentRequirementId, domainId, milestoneId.
- **requirement_update** — id (required), plus any fields to change.
- **requirement_list** — optional filters: parentRequirementId, domainId, milestoneId, keyword.
- **requirement_delete** — id (required).

### Standards

- **standard_create** — title (required), description, detailedNotes, scope (enterprise | project).
- **standard_update** — id (required), plus any fields to change.
- **standard_list** — optional filters: scope, keyword.
- **standard_delete** — id (required).

### Work items

- **work_item_create** — title (required), level (Work | Task), description, state, status, priority, assignee (**resource_id or resource slug**), parentId, labels, milestoneId, releaseId.
- **work_item_update** — id (required), plus any fields to change.
- **work_item_list** — optional filters: level, state, status, milestoneId, assignee (**resource_id or resource slug**), parentId.
- **work_item_delete** — id (required).

**Note:** **Tasks are work items with level = Task**; the **task_*** tools are aliases for **work_item_*** constrained to level = Task.

### Work queue

- **work_queue_get** — workItemId (required), **count** (optional); returns ordered queue items for that work item (first N when count provided). Items can be child **work items or tasks**.
- **work_queue_update** — workItemId (required), ordered itemIds (or position list) to set queue order.

### Tasks

- **task_create** — title (required), description, status, priority, assignee (**resource_id or resource slug**), labels, milestoneId, releaseId.
- **task_update** — id (required), plus any fields to change.
- **task_list** — optional filters: status, milestoneId, assignee.
- **task_delete** — id (required).

### Issues

- **issue_create** — title (required), description, state, severity, priority, assignee (**resource_id or resource slug**), workItemId, requirementIds.
- **issue_update** — id (required), plus any fields to change.
- **issue_list** — optional filters: state, severity, priority, assignee, requirementId, workItemId.
- **issue_delete** — id (required).

### Planning

- **milestone_create** / **milestone_update** / **milestone_list**
- **release_create** / **release_update** / **release_list**

### Domains

- **domain_create** — name (required), description.
- **domain_update** — id (required), plus any fields to change.
- **domain_list** — optional filters: keyword.
- **domain_delete** — id (required).

### Systems

- **system_create** — name (required), category (Application | Framework | API | Compound), description.
- **system_update** — id (required), plus any fields to change.
- **system_list** — optional filters: category, keyword.
- **system_delete** — id (required).

### Assets

- **asset_create** — name (required), assetTypeId, urn, thumbnailAssetId, description.
- **asset_update** — id (required), plus any fields to change.
- **asset_list** — optional filters: assetTypeId, keyword.
- **asset_delete** — id (required).

### Resources

- **resource_create** — name (required), description, oauth2Sub.
- **resource_update** — id (required), plus any fields to change.
- **resource_list** — optional filters: keyword.
- **resource_delete** — id (required).

### Keywords

- **keyword_create** — label (required).
- **keyword_update** — id (required), label.
- **keyword_list** — optional filters: label.
- **keyword_delete** — id (required).

### Associations and dependencies

- **project_dependency_add** / **project_dependency_remove** — dependentProjectId, parentProjectId.
- **item_dependency_add** / **item_dependency_remove** — dependentItemId, prerequisiteItemId.
- **work_item_requirement_add** / **work_item_requirement_remove** — workItemId, requirementId.
- **issue_requirement_add** / **issue_requirement_remove** — issueId, requirementId.
- **system_requirement_add** / **system_requirement_remove** — systemId, requirementId, role (included | dependency).
- **resource_team_member_add** / **resource_team_member_remove** — teamId, memberId.
- **entity_keyword_add** / **entity_keyword_remove** — entityType, entityId, keywordId.

All tools return structured results (e.g. JSON) in the tool response content. Errors return a consistent shape (e.g. `{ error: string }`) with `isError: true` where applicable.
