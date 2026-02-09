---
title: Blazor Web App - Implementation Plan
---

# Blazor Web App - Excruciatingly Detailed Implementation Plan

This document provides an **excruciatingly detailed** implementation plan for the **Blazor web application** described in [14 — Project Web App](14-project-web-app.html). It focuses on building a browser-based UI for the Project MCP data model with GitHub OAuth2 authentication, claims-based scope filtering, and CRUD operations over the shared PostgreSQL database.

**References:** [14 — Project Web App](14-project-web-app.html), [03 — Data Model](03-data-model.html), [08 — Identifiers](08-identifiers.html), [06 — Tech Requirements](06-tech-requirements.html), [11 — Implementation Plan](11-implementation-plan.html), [19 — TODO Library Implementation](19-todo-library-implementation.html).

**Note:** **Work items and tasks are the same entity**; a **task** is a **work item with level = Task**.

---

## Gap analysis (review summary)

This plan was reviewed against [14 — Project Web App](14-project-web-app.html), [03 — Data Model](03-data-model.html), [06 — Tech Requirements](06-tech-requirements.html), and [11 — Implementation Plan](11-implementation-plan.html). The following gaps were identified and filled with excruciating detail below:

| Gap | Remediation |
|-----|-------------|
| **Scope resolution source** | Allowed enterprises/projects must be resolved from the database after GitHub callback (e.g. via resource → mapping table); claim names and flow specified in §3. |
| **UserScope shape** | Defined as `AllowedEnterpriseIds`, `AllowedProjectIds`, and optional `CurrentResourceId` (for "assigned to me"); every service receives it and filters all queries. |
| **Missing services** | Added IRequirementService, IStandardService, IDomainService, ISystemService, IAssetService, IResourceService; full list in §4. |
| **Tree structure** | Full node-type hierarchy and lazy-load API per [14 §4.1](14-project-web-app.html); children per node type specified. |
| **Routes** | Added /requirement/{id}, /standard/{id}, /domain/{id}, /system/{id}, /asset/{id}, /resource/{id}; breadcrumb and ScopeGuard copy. |
| **Detail views** | Per-entity fields and linked entities (Enterprise, Requirement, Standard, Domain, Asset, Resource, System); work queue, dependencies, assignees for work items. |
| **Search** | Keyword AND/OR; FTS schema (tsvector/GIN migration); optional filters; optional keyword cloud. |
| **Reports** | Three report types (milestone, project, resource workload); selectors; chart approach. |
| **Gantt** | Dependency arrows, milestone/release markers, time range selector, filters; API shape for dates and dependencies. |
| **Issues** | List columns; "Issues" section from requirement/work item detail; optional board view. |
| **SUDO** | Explicit step: enforce SUDO on create-enterprise endpoint (403 if not SUDO). |
| **No claim = no access** | ScopeGuard shows "no projects assigned" when claims are empty; no data returned. |
| **Logging** | Correlation id, session/resource in log context; exception Data and inner exceptions per [06](06-tech-requirements.html); no secrets. |
| **Dashboard** | Content: my tasks, open issues, milestone deadlines, recent activity (configurable widgets). |
| **Resource resolution** | Resolve current user's resource (GitHub user id → resources.oauth2_sub) for "assigned to me" and workload. |
| **Testing** | Unit (UserScope, scope enforcement); integration (auth callback, scope filtering); optional E2E. |
| **API contract** | When API is added: pagination, error shape, scope_slug per [14 §11](14-project-web-app.html). |
| **Docker** | Base path for reverse proxy; optional HEALTHCHECK. |

---

## 1. Goals and scope

### 1.1 Goals
- Provide a production-ready Blazor web UI for managing project data.
- Authenticate users with GitHub OAuth2 and enforce claims-based enterprise/project scope.
- Use the same PostgreSQL database and data model as the MCP server.
- Align with **Phase 10** of [11 — Implementation Plan](11-implementation-plan.html); depends on Phase 8 (GitHub OAuth2) and Phase 9 (extended entities if the web app uses requirements, standards, domains, systems, assets, resources, keywords beyond v1).

### 1.2 Out of scope (v1)
- MCP transport behavior (stdio/REST) and agent tooling.
- Mobile UI (covered in [15 — Mobile App](15-mobile-app.html)).
- **Exports:** CSV/PDF export of reports or Gantt (later enhancement per [14 §10](14-project-web-app.html)).

**In scope for V1:** **Audit logging** (change tracking recording and read-only audit/history API per [06 — Tech Requirements](06-tech-requirements.html)). The web app may show "last changed" and optional history per entity using that API.

---

## 2. Solution and project structure

### 2.1 Project layout

| Project | Type | Purpose |
|---------|------|---------|
| **ProjectMcp.WebApp** | Blazor Web App (.NET 10) | UI, auth, API endpoints (if needed). Root namespace `ProjectMCP` per [06 — Tech Requirements](06-tech-requirements.html). |
| **ProjectMcp.TodoEngine** | Class library | Domain/data access reused by WebApp; exposes View (ITodoView), repositories, and DbContext per [19 — TODO Library Implementation](19-todo-library-implementation.html). |

**Step 2.1.1:** Add `ProjectMcp.WebApp` to the solution. Target `net10.0`; use the unified Blazor Web App template and enable Server (and optionally WebAssembly) render mode as required.

**Step 2.1.2:** WebApp **reuses** ProjectMcp.TodoEngine: call `AddTodoEngine(services, configuration)` (or register the same DbContext and repositories) so that the web app uses the same schema and, where applicable, the **same View (ITodoView)** for CRUD to avoid duplicating business logic. If the host prefers, services in the WebApp can wrap the View and add scope filtering (UserScope) before/after calling the View.

### 2.2 Rendering model

**Decision:** Default to **Blazor Web App** with **Server** render mode for authenticated pages. If interactivity or offline support is required later, add WebAssembly render mode for specific components. For Blazor Server, `HttpContext.User` is the same as the cookie principal; use `AuthorizeView` or `[Authorize]` on pages.

---

## 3. Authentication and authorization

### 3.1 GitHub OAuth2

**Step 3.1.1:** Add authentication packages:
- `Microsoft.AspNetCore.Authentication.Cookies`
- `Microsoft.AspNetCore.Authentication.OAuth` (or `AspNetCore.Authentication.GitHub`)

**Step 3.1.2:** Configure in `Program.cs`:
- **Cookie:** Default scheme and DefaultSignInScheme; for production set SecurePolicy, sliding expiration (e.g. 14 days), SameSite. Store claims in cookie (or server-side session if payload is large).
- **GitHub OAuth:** ClientId/ClientSecret from `GitHub:ClientId`, `GitHub:ClientSecret` (or env `GITHUB_CLIENT_ID`, `GITHUB_CLIENT_SECRET`). **CallbackPath** = `/signin-github` (or `GitHub:CallbackPath`); must match GitHub OAuth App's "Authorization callback URL" exactly (e.g. `https://your-app/signin-github`).
- **Scopes:** Add `read:user` (and optionally `user:email`) so `/user` returns id and profile.

**Step 3.1.3:** In **OnCreatingTicket** (after code exchange):
- Map GitHub **user id** (numeric) to `ClaimTypes.NameIdentifier` (string) — used as oauth2_sub for Resource resolution.
- Map **login** to `ClaimTypes.Name`.
- **SUDO:** If user id is in `PROJECT_MCP_SUDO_GITHUB_IDS` (comma-separated), add claim `ClaimTypes.Role` = `"SUDO"`.
- **Scope:** Resolve allowed enterprises and projects from DB (e.g. Resource by oauth2_sub → mapping table or assignments). Add claims `allowed_enterprises` and `allowed_projects` (e.g. JSON arrays of GUIDs). If none, add empty arrays so user sees no data until scope is assigned.

### 3.2 Scope enforcement (claims-based)

**Step 3.2.1:** Create a `UserScope` service that reads allowed enterprise/project ids from claims (or resolves from DB).

**Step 3.2.2:** All data services must accept a `UserScope` and apply it to queries. Do not rely on UI filtering alone.

**Step 3.2.3:** Reject out-of-scope requests with 403 and log details (user id, endpoint, requested ids, target enterprise) for manual follow-up.

### 3.3 UserScope type and service

**Step 3.3.1:** Define **UserScope** (record or class): **AllowedEnterpriseIds** (`IReadOnlyList<Guid>`), **AllowedProjectIds** (`IReadOnlyList<Guid>`), **CurrentResourceId** (`Guid?`). CurrentResourceId is resolved from `resources.oauth2_sub` = GitHub user id for "assigned to me" and workload.

**Step 3.3.2:** Create **IUserScopeService** that returns UserScope from ClaimsPrincipal (claims or DB lookup). Register scoped; inject into pages and services. All services filter by UserScope on every query.

**Step 3.3.3:** **No claim = no access:** If allowed enterprises/projects are empty, show "You have no projects assigned" (ScopeGuard); do not render data.

**Step 3.3.4:** **SUDO:** Only users with role SUDO may create enterprises; enforce on create-enterprise endpoint (403 otherwise). **Cross-enterprise attempts:** Reject with 403; log user id, endpoint, requested ids, target enterprise for manual follow-up.

---

## 4. Data access and services

### 4.1 DbContext and repositories

**Step 4.1.1:** Reuse **ProjectMcp.TodoEngine** DbContext and repositories (and, where applicable, ITodoView) so the web app uses the same schema and business logic as the MCP server. Register via `AddTodoEngine(services, configuration)` or by registering the same DbContext and repositories with the WebApp's DI.

**Step 4.1.2:** Connection string: read from config in order (1) `PROJECT_MCP_CONNECTION_STRING`, (2) `DATABASE_URL`, (3) `ConnectionStrings__DefaultConnection`. **Fail fast on startup** if no connection string is configured: throw with a clear message so the app does not run with a missing DB.

### 4.2 Service layer

**Step 4.2.1:** Create domain services that **accept UserScope** (or resolve it from IUserScopeService) and enforce scope on every call. Each service wraps TodoEngine View/repositories and applies `AllowedEnterpriseIds` / `AllowedProjectIds` to queries. Full list:
- `IEnterpriseService` — enterprises (SUDO required for create).
- `IProjectService` — projects, sub-projects, project dependencies.
- `IRequirementService` — requirements (parent/child, domain, milestone).
- `IStandardService` — standards (enterprise- and project-level).
- `IWorkItemService` — work items and tasks (level = Work | Task); filters by project and scope.
- `IMilestoneService` — milestones (enterprise scope).
- `IReleaseService` — releases (project scope).
- `IIssueService` — issues (state, assignee, requirement/work item links).
- `IDomainService` — domains (enterprise scope).
- `ISystemService` — systems (enterprise scope); system–requirement associations.
- `IAssetService` — assets (enterprise scope); asset types.
- `IResourceService` — resources (enterprise scope); team members; resolve by oauth2_sub.
- `IKeywordService` — keywords (enterprise scope); entity–keyword links.
- `ISearchService` — full-text and keyword search; results filtered by UserScope.
- `IReportsService` — milestone/project/resource progress aggregates; scope applied.

**Step 4.2.2:** Services expose **DTOs** to the UI; do not return EF entities directly. Use CurrentResourceId from UserScope for "assigned to me" and workload queries (e.g. tasks where resource_id = CurrentResourceId).

### 4.3 Current user resource (assigned to me)

**Step 4.3.1:** Resolve the **current user's Resource** by matching `User.FindFirst(ClaimTypes.NameIdentifier)?.Value` (GitHub user id) to `resources.oauth2_sub` in the database. Store in UserScope as **CurrentResourceId**. Use for: "My tasks," workload report, issue assignee default, and task queue filtered by assignee.

---

## 5. UI architecture

### 5.1 Layout

**Step 5.1.1:** Create a shared layout with:
- Header: search box, user profile, enterprise switcher.
- Sidebar: tree navigation.
- Main content: routed pages.

**Step 5.1.2:** Add a `ScopeGuard` component that shows an “access denied” or “no projects assigned” state if the user has no scope. When scope is empty, show "You have no projects assigned" and do not render tree or data. Add breadcrumb in main content (enterprise → project → entity).

### 5.2 Routing

**Step 5.2.1:** Define routes (all require auth; resolve id and validate entity is in UserScope before rendering):
- `/` — Dashboard. `/enterprise/{id}`, `/project/{id}`, `/requirement/{id}`, `/standard/{id}`, `/work-item/{id}`, `/task/{id}`, `/milestone/{id}`, `/release/{id}`, `/issue/{id}`, `/domain/{id}`, `/system/{id}`, `/asset/{id}`, `/resource/{id}`, `/search`, `/reports`, `/gantt`.

---

## 6. Tree navigation

**Step 6.1:** Implement **TreeService** (or equivalent) that loads children **lazily per node type** per [14 §4.1](14-project-web-app.html). Expose methods such as `GetChildren(nodeType, id, UserScope)` so the tree only loads children when a node is expanded. Structure:
- **Enterprise (root):** Children = **Projects** (list), **Resources** (list), **Domains** (list), **Assets** (list), **Standards** (enterprise-level), **Milestones** (list), **Systems** (list). Only enterprises in UserScope.AllowedEnterpriseIds are shown as roots.
- **Project:** Children = **Requirements** (parent/child tree), **Standards** (project-level), **Work** (work items with level = Work; each expands to **Work items** and **Tasks**), **Releases**, **Sub-projects** (recursive: same structure).
- **Domain:** Expanding shows **Requirements** in that domain (read-only).
- **Resource (team):** Expanding shows **Members** (resource_team_members).
- **Milestone:** Expanding shows **Requirements** (and inferred projects) tied to that milestone.
- **System:** Expanding shows **Requirements** (included / dependency via system_requirement_associations).

**Step 6.2:** Tree nodes display **counts** and **status badges** when available (e.g. task count, milestone state). Pagination or "load more" for large lists (e.g. tasks under a work item).

**Step 6.3:** Clicking a node navigates to its **detail page** (route by entity type and id) and loads the relevant DTO; validate entity is in UserScope before rendering.

---

## 7. Entity detail views (CRUD)

### 7.1 Read views (per-entity fields and linked entities)

**Step 7.1.1:** For each entity type, render key fields and linked entities (per [14 §4.3](14-project-web-app.html)):
- **Enterprise:** Name, description; links/counts to projects, resources, domains, assets, milestones, standards.
- **Project:** Name, description, status, tech stack; tabs/sections for requirements, work, releases, sub-projects.
- **Requirement:** Title, description, **parent/children**, **domain**, **milestone**, linked **work items**, linked **standards**.
- **Standard:** Title, description, detailed notes; scope (enterprise | project); linked entities if needed.
- **Work item (level = Work):** Title, dates, effort, priority, **state**, **assignee(s)** (resource), **requirements**, **dependencies** (item_dependencies), **work queue** (ordered items). Child work items and tasks listed.
- **Task (level = Task):** Title, **status** (todo / in-progress / done / cancelled), assignee, milestone, release, requirements, dependencies; same entity as work item, task-specific UI.
- **Milestone:** Title, due date, state, description; requirements and projects in scope.
- **Release:** Name, tag version, date, notes; project.
- **Issue:** Title, description, **state**, severity, priority, **assignee**, **linked requirements**, **work item**; "Issues" section also from requirement or work item detail.
- **Domain, System, Asset, Resource:** Key fields per [03 — Data Model](03-data-model.html); Domain/System show linked requirements; Resource shows team members if team.

### 7.2 Create/update forms

**Step 7.2.1:** Use Blazor `EditForm` with validation; bind to DTOs; validate entity is in UserScope before save.

**Step 7.2.2:** **Work items vs tasks:** Task create/edit view always sets `level = Task`. Work item create/edit view sets `level = Work`. Do not expose level toggle on task UI.

### 7.3 Delete behavior

**Step 7.3.1:** For destructive actions, require **explicit confirmation** (e.g. modal). Document cascade behavior (e.g. deleting a work item with children: reject or cascade per product rule).

---

## 8. Search

### 8.1 Keyword search

**Step 8.1.1:** Implement keyword selection using `IKeywordService` (keywords scoped to current enterprise). UI: dropdown or tag input; user selects one or more keywords from the enterprise.

**Step 8.1.2:** Filter results by UserScope and selected keywords. Support **AND** (entities that have all selected keywords) or **OR** (any of them); make configurable (e.g. toggle or setting). Results: list or tree of matching entities with links to detail views.

**Step 8.1.3:** Optional: "Keyword cloud" or list of most-used keywords for the current enterprise to aid discovery.

### 8.2 Full-text search

**Step 8.2.1:** **Schema:** Add (or use) **tsvector** columns and **GIN indexes** on searchable tables (e.g. name, title, description) per [14 §6](14-project-web-app.html) and [11 Phase 10](11-implementation-plan.html) (task 10.3). Use a migration; optionally a single search view/table that aggregates entity_type, entity_id, and searchable text.

**Step 8.2.2:** Implement `ISearchService`: PostgreSQL full-text search (e.g. `websearch_to_tsquery`); **restrict results to UserScope** (allowed enterprises/projects). Return results **grouped by entity type** with **snippets** and link to detail view.

**Step 8.2.3:** **UI:** Global search box (header); run on Enter or debounce. Optional filters: entity type, date range, project, assignee. All filters must respect UserScope.

---

## 9. Reports and Gantt

### 9.1 Reports

**Step 9.1.1:** Implement **IReportsService** (or ReportsService) with three report types per [14 §7](14-project-web-app.html):
- **Milestone progress:** For selected milestone: requirements and linked work; % of work items/tasks in Complete (or state distribution); progress bar per requirement or overall.
- **Project progress:** By project: counts of requirements, tasks (todo / in-progress / done), work items by state; optional roll-up from sub-projects.
- **Resource workload:** Per resource (or team): count/list of assigned tasks and work items; optional effort vs capacity.

**Step 9.1.2:** **Selectors:** Enterprise, project, milestone, date range, resource — all options filtered by UserScope. **Charts:** Use a Blazor-friendly chart library (e.g. Chart.js via JS interop or .NET chart component); progress bars, pie/bar charts, tables with percentages.

### 9.2 Gantt

**Step 9.2.1:** Choose a Gantt component (Blazor or JS library via interop). **Data:** Work items and tasks with **start_date**, **deadline** (or end date); **item_dependencies** for arrows (task A → task B). Show **milestone** and **release** markers as vertical lines or labels.

**Step 9.2.2:** **Service/endpoint:** Return for selected project (and optional milestone/work item): list of work items and tasks with dates and **dependency list** (dependent_id, prerequisite_id). Scope to UserScope.

**Step 9.2.3:** **UI:** Time range selector (week / month / quarter). Optional filters: assignee, requirement, work item state. Interactive: click bar for detail; optional drag to reschedule if supported.

---

## 10. Issue tracking

**Step 10.1:** **Issue list:** Table or cards; columns: title, state, assignee, linked requirements, work item, created/updated. **Filters:** state, assignee, project (via requirement), requirement. All filtered by UserScope.

**Step 10.2:** **Issue detail:** Full form (title, description, state, severity, priority, requirements, work item, assignee). Create/update; link to requirement(s) and work item. From **requirement** or **work item** detail view, show an **"Issues"** section listing issues linked to that requirement or work item.

**Step 10.3:** Optional: **Board view** — columns by state (Open, In progress, Done, Closed); drag-and-drop to change state.

---

## 11. API surface (if needed)

If WebAssembly or external clients require an HTTP API:
- Add **minimal APIs or controllers** under `/api`. Secure with the same cookie/claims-based auth (or Bearer if issuing tokens).
- Reuse the same services; do not duplicate business logic.
- **API contract** per [14 §11](14-project-web-app.html): **Scope:** requests include **scope_slug** (or equivalent) to select perspective; server resolves slug and applies UserScope. **Pagination:** `page` + `page_size` (or cursor) on list endpoints; return total/count metadata. **Errors:** JSON shape `{ error: string, code?: string }`; 401/403/404 as appropriate.

---

## 12. Logging and error handling

**Step 12.1:** Add **Serilog** (or configured structured logging) per [06 — Tech Requirements](06-tech-requirements.html). **All log records** must include when available: **session identification** (e.g. request/session id), **resource/user identification** (e.g. user id or resource_id), and **correlation id** (if provided, e.g. from header). Attach to log context per request. **Logged exceptions:** Add exception **Data** name-value pairs as structured properties; log **inner exceptions** recursively (one log entry per exception in chain). **No secrets** in logs.

**Step 12.2:** Log **auth events** (sign-in, sign-out, failure), **scope violations** (user id, endpoint, requested ids, target enterprise for manual follow-up), and **data write operations** (entity, operation, user).

**Step 12.3:** Surface **validation errors** to users with clear messages; do not leak stack traces or secrets.

---

## 13. Configuration and secrets

**Step 13.1:** Required env/config keys:
- `GITHUB_CLIENT_ID`, `GITHUB_CLIENT_SECRET` (or `GitHub:ClientId`, `GitHub:ClientSecret`).
- `GitHub:CallbackPath` (e.g. `/signin-github`); must match GitHub OAuth App "Authorization callback URL" exactly.
- `PROJECT_MCP_SUDO_GITHUB_IDS` (comma-separated GitHub user ids for SUDO role).
- `PROJECT_MCP_CONNECTION_STRING` or `DATABASE_URL` or `ConnectionStrings__DefaultConnection`.
- `ASPNETCORE_URLS` (e.g. `http://localhost:5001`) for listen URL. Optional: base path if app is behind reverse proxy at a subpath.

**Step 13.2:** No secrets in repo; use env or secret store. Document all keys in README or deployment docs.

---

## 14. Docker and hosting

**Step 14.1:** Add **Dockerfile** for `ProjectMcp.WebApp`: multi-stage build with `mcr.microsoft.com/dotnet/sdk:10.0`, publish; runtime image `mcr.microsoft.com/dotnet/aspnet:10.0`. Copy published output; **ENTRYPOINT** to run the web app (e.g. `dotnet ProjectMcp.WebApp.dll`). **EXPOSE** HTTP port (e.g. 5001 or from `ASPNETCORE_URLS`).

**Step 14.2:** Container reads **auth** (GitHub client id/secret, SUDO list) and **DB** connection string from **env** at runtime; no secrets in image.

**Step 14.3:** If the app is served behind a **reverse proxy** at a subpath, configure **base path** (e.g. `PathBase`) so links and redirects work. Optional: **HEALTHCHECK** in Dockerfile (e.g. HTTP GET to `/health` or similar) for orchestrator liveness.

---

## 15. Dashboard (home)

**Step 15.1:** After login or enterprise selection, show a **dashboard** per [14 §10](14-project-web-app.html) with configurable widgets:
- **My tasks** — Tasks (work items with level = Task) where assignee = CurrentResourceId (UserScope); optional status filter.
- **Open issues** — Issues in Open or InProgress assigned to current user or in scoped projects.
- **Milestone deadlines** — Upcoming milestones (due date) for selected enterprise/project.
- **Recent activity** — Optional: recent changes (if audit/history is available) or recently updated work items/issues.

**Step 15.2:** Selectors (enterprise, project) on dashboard are limited to UserScope. Data is read via same scoped services.

---

## 16. Testing

**Step 16.1:** **Unit tests:** UserScope resolution from claims (and from DB when claims empty); scope enforcement in service layer (e.g. reject request for entity in another enterprise). Mock IUserScopeService and assert 403 or empty result when out of scope.

**Step 16.2:** **Integration tests:** Auth callback flow (GitHub code exchange, claims populated, redirect); one or more pages that load data with UserScope and assert only scoped entities returned. Use Testcontainers Postgres or test DB; no production data.

**Step 16.3:** **Optional E2E:** Sign-in with test GitHub user, select enterprise, open tree and detail views, run search; assert no cross-enterprise data. Can be automated or manual; document in [12 — Testing Plan](12-testing-plan.html) if WebApp tests are added there.

---

## 17. Acceptance checklist

- [ ] User can sign in with GitHub and view **scoped data only** (tree, list, detail).
- [ ] **ScopeGuard** shows "You have no projects assigned" when scope is empty; tree and data not rendered.
- [ ] **SUDO** user can create enterprise; non-SUDO cannot (403 on create enterprise).
- [ ] CRUD operations **enforce scope** and **task/work item semantics** (level = Task vs Work).
- [ ] **Search** (keyword and full-text), **reports**, and **Gantt** reflect the scoped dataset only.
- [ ] **Dashboard** shows my tasks and open issues (when CurrentResourceId resolved) and milestone deadlines.
- [ ] **No secrets** in configuration or logs; cross-enterprise attempts **logged** for manual follow-up.
