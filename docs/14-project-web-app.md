---
title: Project Web App
---

# Project Web App — Website Specification

This document defines a **web application** that users can visit to browse and manage project data: tree navigation, keyword and full-text search, progress reports, Gantt charts, issue tracking, and related features. The app shares the same [data model](03-data-model.html) and PostgreSQL backend as the MCP server. It is implemented in **Blazor** on **.NET 10** and hosted as a **Docker** container.

---

## 1. Purpose and scope

- **Purpose:** Provide a browser-based UI for viewing and editing enterprise/project data (projects, requirements, work, tasks, milestones, releases, resources, domains, assets, issues, keywords) without using the MCP or an AI client.
- **Users:** Humans (project managers, team members, stakeholders) who need to navigate hierarchy, search entities, track progress, view timelines, and manage issues.
- **Scope:** Read and write access to the same entities and relationships as the [data model](03-data-model.html); methodology-agnostic. **Work items and tasks are the same entity** (task = work item with level = Task). The web app and the MCP server are separate entry points to the same database.
- **Authentication:** Users are authenticated with **OAuth2** using **GitHub** as the provider. Their **visibility into projects and enterprises is filtered by claims in their token**; see §2.
- **Out of scope for this doc:** Mobile-specific UX, real-time collaboration.

---

## 2. Authentication and authorization

### 2.1 Authentication: GitHub OAuth2

- **Provider:** **GitHub** is used as the **OAuth2 provider**. The web app does not store passwords; it delegates sign-in to GitHub.
- **Protocol:** Users authenticate via **OAuth2** (GitHub’s [OAuth 2.0 flow](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps)). The app uses GitHub’s authorization and token endpoints; the access token (and optionally the GitHub API) is used to obtain the user’s identity (e.g. GitHub user id or login) for resolving the user to a [resource](03-data-model.html) (`oauth2_sub`) and for any custom claims (enterprise/project scope) if supplied by a backend or token transformation.
- **Typical flow:** User hits the app → redirected to **GitHub** → signs in (or authorizes the app) → GitHub redirects back with an authorization code → app exchanges code for an access token → app uses the token (and optionally calls GitHub API `/user` to get user id) and establishes an authenticated session. Custom claims (e.g. enterprise_id, project_id) may be added by the app backend after validating the GitHub token and looking up the user’s scope in the database.
- **Implementation:** Use ASP.NET Core **authentication middleware** with GitHub (e.g. `AddOAuth` for GitHub, or a middleware that validates the GitHub access token and builds a `ClaimsPrincipal` with the user’s GitHub id and any app-defined claims). For Blazor Server, the validated token is used to populate `HttpContext.User`; for Blazor WebAssembly calling an API, send the access token in the `Authorization` header and validate it in the API. Token refresh (if supported by the app’s token store) for long-lived sessions.
- **Configuration:** GitHub OAuth App **client id** and **client secret**, **callback URL**, and **scopes** (e.g. `read:user`, `user:email`) are supplied via configuration (e.g. environment variables or appsettings); no secrets in the image.

### 2.2 Authorization: claims-based visibility

- **Principle:** A user may only see and act on **enterprises** and **projects** (and their child entities) for which they have a **claim** in their token. All data access in the web app must be **filtered** using these claims; the backend never returns data outside the user’s visible scope.
- **Every endpoint must validate enterprise scope:** Every API and web endpoint must **check that any data requested or submitted is within the enterprise(s) the user is associated with** (via claims). For reads: return only entities whose enterprise (or project’s enterprise) is in the user’s allowed set. For writes: reject requests that would create or change data in another enterprise. **Any attempt to access data of another enterprise** must be **rejected** (e.g. 403), **logged** with sufficient detail (user id, endpoint, requested ids, target enterprise) for manual follow-up, and **followed up manually** by operators. See [06 — Tech Requirements](06-tech-requirements.html) (Security and safety).
- **Claims in the token:** The app builds or receives claims that describe the user’s allowed scope. GitHub provides the user’s identity (e.g. id, login); **enterprise and project scope** (e.g. `enterprise_id`, `project_id` or `allowed_enterprises`, `allowed_projects`) are typically **app-defined**: the backend resolves the GitHub user to a resource and looks up the user’s allowed enterprises/projects (e.g. from a mapping table or from the resource’s assignments) and adds those as claims, or the app filters all queries by a scope derived from the database after validating the GitHub token. Examples (exact claim names and shape are configurable):
  - **Enterprise IDs:** e.g. `enterprise_id` (multi-valued) or `allowed_enterprises` (JSON array of GUIDs). The user sees only those enterprises.
  - **Project IDs:** e.g. `project_id` (multi-valued) or `allowed_projects` (JSON array of GUIDs). The user sees only those projects (and their parent enterprises if allowed). Alternatively, scope could be expressed only at enterprise level (user sees all projects under allowed enterprises).
  - **Roles (optional):** e.g. `role` or `project_role` (e.g. `admin`, `member`, `viewer`) per enterprise or project to drive create/update/delete permissions; if not present, treat as “viewer” or “member” per deployment policy.
- **SUDO role:** The **SUDO** role is a special role (e.g. claim `role: SUDO` or from an app-defined role mapping). **Only users with the SUDO role may add (create) enterprise records.** All other users cannot create enterprises; they may only view and manage data within enterprises/projects for which they have scope. The backend must enforce this on any “create enterprise” operation (e.g. return 403 if the user does not have SUDO). **Source of truth:** env `PROJECT_MCP_SUDO_GITHUB_IDS` (comma-separated GitHub user ids) used to grant SUDO at sign-in.
- **MCP agent identity:** MCP clients (e.g. Copilot/Cursor) must resolve their agent name to a **Resource** in the enterprise; otherwise requests return **Unauthorized. Agent not approved for Enterprise** (see [04 — MCP Surface](04-mcp-surface.html)).
- **How filtering is applied:**
  - **Tree:** Load enterprises and projects only for IDs present in the user’s claims (or for enterprises that contain at least one allowed project). Children (requirements, work, tasks, etc.) are loaded only when their parent project/enterprise is in scope.
  - **Search (keyword and full-text):** Restrict queries with a predicate such as `enterprise_id IN (user's allowed enterprises)` and `project_id IN (user's allowed projects)` so results only include entities the user is allowed to see.
  - **Reports, Gantt, issues:** Same rule: selectors (enterprise, project, milestone, resource) only offer choices within the user’s claimed scope; underlying queries filter by the same enterprise/project sets.
- **No claim = no access:** If the token has no enterprise or project claims (or they are empty), the user sees no data (empty tree and search results); optionally show a message that they have no projects assigned.
- **Implementation:** After validating the token, the app (or a shared service) **resolves** the user’s allowed enterprise IDs and project IDs from claims and caches them for the request/session. The **current user’s resource** (for “assigned to me,” workload, etc.) can be resolved by matching the **GitHub user id** (from the token or GitHub API) to a resource’s **oauth2_sub** in the [data model](03-data-model.html) (resources can have an OAuth2 id). Every data service (e.g. `IProjectService`, `ISearchService`) receives this **scope** (or the `ClaimsPrincipal`) and applies it to all queries. Do not rely on the UI to hide data; enforce at the API/data layer.

### 2.3 Summary

| Aspect | Detail |
|--------|--------|
| **Auth** | **GitHub** as OAuth2 provider; no passwords in the app. |
| **Visibility** | Filtered by claims in the token (e.g. `enterprise_id`, `project_id` or `allowed_enterprises`, `allowed_projects`). |
| **SUDO role** | Only users with the **SUDO** role may create enterprise records; enforced in the backend. |
| **Enforcement** | All tree, search, reports, Gantt, and issue data are queried with a scope derived from the user’s claims; backend never returns out-of-scope data. Every endpoint validates data is within the user’s enterprise(s). |
| **Cross-enterprise attempts** | Logged with detail for manual follow-up; request rejected (e.g. 403). |

---

## 3. Technology stack

| Layer | Choice | Notes |
|-------|--------|--------|
| **Authentication** | GitHub OAuth2 | Validate GitHub access token; claims (GitHub user + app-defined scope) drive visibility (see §2). |
| **UI framework** | Blazor | .NET 10. Prefer **Blazor Web App** (unified model) with optional **Blazor WebAssembly** for interactivity and **Blazor Server** or **Auto** render mode as needed. |
| **Runtime** | .NET 10 | LTS when released; target .NET 10 in csproj. |
| **Backend** | ASP.NET Core (same process or API) | Blazor Web App hosts pages; data access via services that use the same PostgreSQL as the MCP server. |
| **Data access** | EF Core + Npgsql (or shared library with MCP) | Same schema as [03 — Data Model](03-data-model.html). Optionally share a **core** library (entities, DbContext) between MCP server and web app. |
| **Hosting** | Docker | Single container for the web app; connects to PostgreSQL (same DB as MCP or dedicated instance). |
| **Search (full-text)** | PostgreSQL `tsvector` / `websearch_to_tsquery` or dedicated search index | Full-text search over entity fields; see §7. |

---

## 4. Tree navigation

Users can navigate the data in a **tree** whose root is the **enterprise**. The tree reflects the ownership and containment rules in the [data model](03-data-model.html) and [00 — Definitions](00-definitions.html).

### 4.1 Tree structure (outline)

- **Enterprise** (root)
  - **Projects** — List of projects owned by the enterprise. Expanding a project shows:
    - **Requirements** (parent/child tree)
    - **Standards** (project-level)
    - **Work** — Each work node can expand to:
      - **Work items** (with state, task ordering, work queue)
      - **Tasks** (with sub-tasks and dependencies)
    - **Releases**
    - **Sub-projects** (recursive: same structure)
  - **Resources** — Enterprise-level list (people, teams, tools). A resource that is a team can expand to show **members**.
  - **Domains** — Enterprise-level list. Expanding a domain shows **requirements** in that domain (read-only association).
  - **Assets** — Enterprise-level list (documentation, diagrams, media). Optional: expand to show type and thumbnail.
  - **Standards** — Enterprise-level standards (if any).
  - **Milestones** — Enterprise-level list; expanding shows **requirements** (and inferred projects) tied to that milestone.
  - **Systems** — Enterprise-level list; expanding shows **requirements** (included / dependency).

### 4.2 UI behavior

- **Sidebar or left panel:** Collapsible tree; one pane per “branch” (e.g. Projects, Resources, Domains, Assets) or a single tree with all branches under the selected enterprise.
- **Selection:** Clicking a node loads the **detail view** for that entity in the main content area (e.g. project dashboard, requirement card, task form, resource profile).
- **Breadcrumb:** Show path from enterprise → project → requirement (or work, task, etc.) for context.
- **Lazy load:** Load children on expand (do not load entire tree at once). Pagination or “load more” for large lists (e.g. tasks under a work item).
- **Multi-enterprise:** The user selects an enterprise from those **allowed by their token claims** (e.g. dropdown or switcher); the tree root is that enterprise. Only enterprises and projects present in the user’s claims are shown.

### 4.3 Detail views (summary)

- **Enterprise:** Name, description; links to counts or lists of projects, resources, domains, assets, milestones.
- **Project:** Name, description, status, tech stack; tabs or sections for requirements, work, releases, sub-projects.
- **Requirement:** Title, description, parent/children, domain, milestone, linked work/standards.
- **Work / Work item / Task:** Title, dates, effort, priority, state, assignee(s), requirements, dependencies, work queue (for work item).
- **Resource:** Name, description, team members (if team).
- **Domain / Asset / Milestone / Release / System:** Key fields and related entities per [data model](03-data-model.html).
- **Issue:** Title, description, state, linked requirements, work item, assignee.

---

## 5. Keyword search

- **Scope:** Keywords are scoped to a single enterprise ([03 — Data Model](03-data-model.html)); entity–keyword is stored in `entity_keywords`.
- **UI:** A **keyword search** control (e.g. dropdown or tag input) that:
  - Lets the user type or select one or more **keywords** (from the current enterprise).
  - Filters the current view or runs a search that returns **entities that have all selected keywords** (AND) or any of them (OR), configurable.
- **Results:** List or tree of matching entities (projects, requirements, tasks, etc.) with links to their detail views.
- **Optional:** “Keyword cloud” or list of most-used keywords for the current enterprise to aid discovery.

---

## 6. Full-text search

- **Scope:** Search across **any entity type** (enterprise, project, requirement, standard, work, work item, task, milestone, release, resource, domain, system, asset, issue) over **text fields** (name, title, description, and other searchable attributes).
- **Backend:** Use PostgreSQL full-text search:
  - Add (or use) `tsvector` columns / generated columns and GIN indexes on the tables that have searchable text (e.g. `name`, `description`, `title`).
  - Single search query that unions results from multiple entity types, or a dedicated **search** table / view that aggregates searchable text with `entity_type` and `entity_id`.
- **UI:** A global **search box** (e.g. in header or sidebar):
  - User types a phrase; suggest or run search (e.g. on Enter or after debounce).
  - Results grouped by **entity type** (e.g. Requirements, Tasks, Issues) with snippet and link to detail view.
  - Optional filters: entity type, date range, project, assignee.
- **Security:** Restrict results to the user’s claims-based scope (allowed enterprises/projects); see §2.

---

## 7. Progress reports

- **Purpose:** Show progress toward milestones, releases, or project goals (e.g. % complete by requirement, by work item state, by task status).
- **Data source:** Same PostgreSQL; aggregate from requirements, work items, tasks, milestones (states and counts).
- **Reports (examples):**
  - **Milestone progress:** For a selected milestone, show requirements and their linked work; % of work items/tasks in “Complete” (or state distribution); progress bar per requirement or overall.
  - **Project progress:** By project: counts of requirements, tasks (todo / in-progress / done), work items by state; optional roll-up from sub-projects.
  - **Resource workload:** Per resource (or team): count or list of assigned tasks/work items; optional effort vs capacity.
- **UI:** Dedicated **Reports** area (page or section) with:
  - Selectors: enterprise, project, milestone, date range, resource.
  - Charts or tables: progress bars, pie/bar charts (e.g. by state), simple tables with percentages.
- **Implementation:** Blazor components plus chart library (e.g. Chart.js via JS interop, or a .NET chart component). Data from API or server-side services that run the aggregates against PostgreSQL.

---

## 8. Gantt charts

- **Purpose:** Visualize work and tasks on a **time axis** (start date, end date / deadline) to show schedule, dependencies, and overlap.
- **Data source:** Work (start_date, deadline), tasks (optional dates or derived from work), task dependencies (**item_dependencies**). Milestones and releases can be shown as markers or swimlanes.
- **Scope:** User selects a **project** (and optionally a milestone or work item) and sees:
  - Rows: work items and/or tasks (optionally grouped by work item or requirement).
  - Timeline: bars for start–end; dependency arrows (task A → task B).
  - Milestone/release markers as vertical lines or labels.
- **UI:** A **Gantt** page or component:
  - Time range selector (week / month / quarter).
  - Optional filters: assignee, requirement, work item state.
  - Interactive: drag to reschedule (if supported), click bar for detail.
- **Implementation:** Use a Gantt component (e.g. JS library via interop, or a Blazor-native Gantt control). Backend exposes work/task list with dates and dependency list for the selected scope.

---

## 9. Issue tracking

- **Purpose:** List, filter, and manage **issues** (defects in planning or implementation) linked to requirements and optionally to work items and assignees ([00 — Definitions](00-definitions.html), [03 — Data Model](03-data-model.html)).
- **Data source:** `issues`, `issue_requirements`; optional link to `work_item` and `resource` (assignee).
- **UI:**
  - **Issue list:** Table or cards with columns: title, state, assignee, linked requirements, work item, created/updated. Filters: state, assignee, project (via requirement), requirement.
  - **Issue detail:** Full form (title, description, state, requirements, work item, assignee). Actions: create, update, assign to work item, change state.
  - **Integration with tree:** From a requirement or work item detail view, show “Issues” section listing issues linked to that requirement or work item.
- **Optional:** Board view (e.g. columns by state: Open, In progress, Done) with drag-and-drop to change state.

---

## 10. Additional features (short list)

- **Dashboard (home):** After login or enterprise selection, show a summary: recent activity, my tasks, open issues, milestone deadlines (configurable widgets).
- **Exports:** Export progress report or Gantt data (CSV, PDF) as a later enhancement.
- **Audit / history:** If the schema adds history tables later, show “last changed” and optional history per entity.

---

## 11. Architecture (web app and backend)

- **Blazor Web App:** Single solution/project (or host + client projects). Pages: Home/Dashboard, Tree (with detail), Search, Reports, Gantt, Issues. Shared layout with header (search, enterprise switcher), sidebar (tree), main content. **Enterprise switcher** and all data are limited to the user’s **claims-based scope** (§2).
- **Data layer:** Reuse or mirror the MCP data model (entities, DbContext). Add full-text search support (migrations for `tsvector`/GIN if not already present). Services: `IEnterpriseService`, `IProjectService`, `ITaskService`, `ISearchService`, `IIssueService`, etc., that wrap EF Core or raw SQL for reports. **Every service receives the current user’s allowed enterprise/project IDs (from token claims) and filters all queries by that scope.**
- **API:** If the Blazor app is WebAssembly or hybrid, expose an **ASP.NET Core minimal API or controllers** for data (JSON). Same server can host both Blazor and API. If Blazor Server only, services can be direct server-side (no separate HTTP API).
- **PostgreSQL:** Same connection string pattern as MCP (e.g. `DATABASE_URL` or `ConnectionStrings__DefaultConnection`). The web app connects to the same database the MCP server uses (or a read replica for read-heavy reports, later).

### API contract (summary)

- **Auth:** GitHub OAuth2 bearer token; reject unauthenticated requests.
- **Scope:** Requests include **scope_slug** (enterprise/project/work item slug) to select perspective; server resolves slug and applies child traversal.
- **Endpoints:** CRUD for projects, requirements, standards, work_items (tasks), issues, milestones, releases, domains, systems, assets, resources, keywords, plus association endpoints.
- **Pagination:** `page` + `page_size` (or `cursor`) on list endpoints; return total/count metadata.
- **Errors:** Standard JSON error shape `{ error: string, code?: string }`, with 401/403/404 as appropriate.

---

## 12. Docker hosting

- **Image:** Build the Blazor app (e.g. `dotnet publish -c Release`) and run it in a container. Use the **ASP.NET Core runtime** image for .NET 10 (e.g. `mcr.microsoft.com/dotnet/aspnet:10.0` when available; until then use `8.0` or preview tag).
- **Dockerfile:** Multi-stage: build stage with SDK, publish stage; copy published output into runtime image; set `ENTRYPOINT` to run the web app (e.g. `dotnet ProjectMcp.Web.dll`).
- **Configuration:** Inject at runtime: `ConnectionStrings__DefaultConnection` (or `DATABASE_URL`), optional `ASPNETCORE_URLS`, base path if behind a reverse proxy. No secrets in the image.
- **Orchestration:** Run the web app container alongside the existing MCP server and PostgreSQL (e.g. same `docker-compose.yml` or Aspire app host). Web app container depends on PostgreSQL; MCP and web app can both connect to the same Postgres instance.
- **Reverse proxy:** In production, put the web app behind a reverse proxy (e.g. nginx, Traefik) for TLS and optional auth; proxy passes requests to the container port (e.g. 8080).

---

## 13. Summary

| Feature | Description |
|---------|-------------|
| **Authentication** | **GitHub** OAuth2; visibility filtered by claims (GitHub user + app-defined enterprise/project scope). |
| **Tree navigation** | Enterprise → Projects \| Resources \| Domains \| Assets → Requirements, Standards, Work (with work items, tasks), releases, milestones, systems. Lazy load; detail view per node; only entities in user’s scope. |
| **Keyword search** | Filter by enterprise keywords (tag selection); results as entity list with links; scoped by claims. |
| **Full-text search** | Global search over all entity text fields (PostgreSQL FTS); results by type with snippet and link; scoped by claims. |
| **Progress reports** | Milestone/project/resource progress (state counts, % complete); selectors and charts; scope from claims. |
| **Gantt charts** | Work/tasks on timeline with dependencies; project/milestone scope; optional drag to reschedule; scope from claims. |
| **Issue tracking** | List, filter, detail, create/update issues; link to requirements and work items; optional board view; scope from claims. |
| **Tech** | Blazor (.NET 10), same PostgreSQL as MCP, Docker container, optional REST API for data. |

This specification defines the website that users can visit to get project information via tree navigation, search, reports, Gantt, and issue tracking, implemented in Blazor on .NET 10 and hosted as a Docker container.
