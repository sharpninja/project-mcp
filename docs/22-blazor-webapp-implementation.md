---
title: Blazor Web App - Implementation Plan
---

# Blazor Web App - Excruciatingly Detailed Implementation Plan

This document provides an **excruciatingly detailed** implementation plan for the **Blazor web application** described in [14 — Project Web App](14-project-web-app.html). It focuses on building a browser-based UI for the Project MCP data model with GitHub OAuth2 authentication, claims-based scope filtering, and CRUD operations over the shared PostgreSQL database.

**References:** [14 — Project Web App](14-project-web-app.html), [03 — Data Model](03-data-model.html), [08 — Identifiers](08-identifiers.html), [06 — Tech Requirements](06-tech-requirements.html), [11 — Implementation Plan](11-implementation-plan.html).

**Note:** **Work items and tasks are the same entity**; a **task** is a **work item with level = Task**.

---

## 1. Goals and scope

### 1.1 Goals
- Provide a production-ready Blazor web UI for managing project data.
- Authenticate users with GitHub OAuth2 and enforce claims-based enterprise/project scope.
- Use the same PostgreSQL database and data model as the MCP server.

### 1.2 Out of scope
- MCP transport behavior (stdio/REST) and agent tooling.
- Mobile UI (covered in [15 — Mobile App](15-mobile-app.html)).

---

## 2. Solution and project structure

### 2.1 Project layout

| Project | Type | Purpose |
|---------|------|---------|
| **ProjectMcp.WebApp** | Blazor Web App (.NET 10) | UI, auth, API endpoints (if needed). |
| **ProjectMcp.TodoEngine** | Class library | Domain/data access reused by WebApp. |

**Step 2.1.1:** Add `ProjectMcp.WebApp` to the solution. If using a unified Blazor Web App, target `net10.0` and enable both server and interactive rendering options as required.

### 2.2 Rendering model

**Decision:** Default to **Blazor Web App** with **Server** render mode for authenticated pages. If interactivity or offline support is required later, add WebAssembly render mode for specific components.

---

## 3. Authentication and authorization

### 3.1 GitHub OAuth2

**Step 3.1.1:** Add authentication packages:
- `Microsoft.AspNetCore.Authentication.Cookies`
- `Microsoft.AspNetCore.Authentication.OAuth` (or `AspNetCore.Authentication.GitHub`)

**Step 3.1.2:** Configure authentication in `Program.cs`:
- Add cookie auth as default scheme.
- Add GitHub OAuth handler with ClientId/ClientSecret from config.
- Set callback path (e.g. `/signin-github`).

**Step 3.1.3:** In `OnCreatingTicket`, map:
- GitHub user id -> `ClaimTypes.NameIdentifier` or `sub`.
- Optional login -> `ClaimTypes.Name`.
- SUDO role if user id is in `PROJECT_MCP_SUDO_GITHUB_IDS`.

### 3.2 Scope enforcement (claims-based)

**Step 3.2.1:** Create a `UserScope` service that reads allowed enterprise/project ids from claims (or resolves from DB).

**Step 3.2.2:** All data services must accept a `UserScope` and apply it to queries. Do not rely on UI filtering alone.

**Step 3.2.3:** Reject out-of-scope requests with 403 and log details (user id, endpoint, target ids).

---

## 4. Data access and services

### 4.1 DbContext and repositories

**Step 4.1.1:** Reuse `ProjectMcp.TodoEngine` DbContext and repositories.

**Step 4.1.2:** Register DbContext with connection string from:
1. `PROJECT_MCP_CONNECTION_STRING`
2. `DATABASE_URL`
3. `ConnectionStrings__DefaultConnection`

### 4.2 Service layer

**Step 4.2.1:** Create domain services that wrap repositories and enforce scope:
- `IEnterpriseService`
- `IProjectService`
- `IWorkItemService`
- `IMilestoneService`
- `IReleaseService`
- `IIssueService`
- `ISearchService`
- `IKeywordService`

**Step 4.2.2:** Services expose DTOs tailored for the UI; do not return EF entities directly.

---

## 5. UI architecture

### 5.1 Layout

**Step 5.1.1:** Create a shared layout with:
- Header: search box, user profile, enterprise switcher.
- Sidebar: tree navigation.
- Main content: routed pages.

**Step 5.1.2:** Add a `ScopeGuard` component that shows an “access denied” or “no projects assigned” state if the user has no scope.

### 5.2 Routing

**Step 5.2.1:** Define routes:
- `/` (dashboard)
- `/enterprise/{id}`
- `/project/{id}`
- `/work-item/{id}`
- `/task/{id}`
- `/milestone/{id}`
- `/release/{id}`
- `/issue/{id}`
- `/search`
- `/reports`
- `/gantt`

---

## 6. Tree navigation

**Step 6.1:** Implement a `TreeService` that loads children lazily:
- Enterprise -> Projects, Resources, Domains, Assets, Milestones, Systems.
- Project -> Requirements, Work Items, Tasks, Releases.

**Step 6.2:** Tree nodes display counts and status badges when available.

**Step 6.3:** Clicking a node navigates to its detail page and loads the relevant DTO.

---

## 7. Entity detail views (CRUD)

### 7.1 Read views

**Step 7.1.1:** For each entity (Project, WorkItem/Task, Requirement, Milestone, Release, Issue):
- Render key fields and relationships.
- Show linked entities (requirements, work items, issues).

### 7.2 Create/update forms

**Step 7.2.1:** Use Blazor `EditForm` with validation.

**Step 7.2.2:** For work items/tasks, enforce `level` correctly:
- Task view always sets `level = Task`.
- Work item view sets `level = Work`.

### 7.3 Delete behavior

**Step 7.3.1:** For destructive actions, require explicit confirmation.

---

## 8. Search

### 8.1 Keyword search

**Step 8.1.1:** Implement keyword selection using `IKeywordService`.

**Step 8.1.2:** Filter results by scope and selected keywords.

### 8.2 Full-text search

**Step 8.2.1:** Implement `ISearchService` using PostgreSQL full-text search per [14 — Project Web App](14-project-web-app.html).

**Step 8.2.2:** Group results by entity type with snippets.

---

## 9. Reports and Gantt

### 9.1 Reports

**Step 9.1.1:** Implement a `ReportsService` to compute milestone/project progress.

**Step 9.1.2:** Render charts using a Blazor-friendly chart library or JS interop.

### 9.2 Gantt

**Step 9.2.1:** Choose a Gantt component (Blazor or JS).

**Step 9.2.2:** Provide an endpoint/service that returns tasks with dates and dependencies for a selected project.

---

## 10. Issue tracking

**Step 10.1:** Issue list page with filters (state, assignee, project).

**Step 10.2:** Issue detail page with create/update and requirement/work item links.

---

## 11. API surface (if needed)

If WebAssembly or external clients are required:
- Add minimal APIs or controllers under `/api`.
- Secure with the same cookie/claims-based auth.
- Reuse services; do not duplicate business logic.

---

## 12. Logging and error handling

**Step 12.1:** Add structured logging (Serilog) for:
- Auth events
- Scope violations
- Data write operations

**Step 12.2:** Surface validation errors to users with clear messages; do not leak secrets.

---

## 13. Configuration and secrets

**Step 13.1:** Required env/config keys:
- `GITHUB_CLIENT_ID`
- `GITHUB_CLIENT_SECRET`
- `PROJECT_MCP_SUDO_GITHUB_IDS`
- `PROJECT_MCP_CONNECTION_STRING` or `DATABASE_URL`

**Step 13.2:** No secrets in repo; use env or secret store.

---

## 14. Docker and hosting

**Step 14.1:** Add Dockerfile for `ProjectMcp.WebApp`:
- Build with `mcr.microsoft.com/dotnet/sdk:10.0`
- Run with `mcr.microsoft.com/dotnet/aspnet:10.0`
- Expose HTTP port (e.g. 5001)

**Step 14.2:** Ensure the container reads auth and DB settings from env.

---

## 15. Acceptance checklist

- [ ] User can sign in with GitHub and view scoped data only.
- [ ] CRUD operations enforce scope and task/work item semantics.
- [ ] Search, reports, and Gantt reflect the scoped dataset.
- [ ] No secrets in configuration or logs.
