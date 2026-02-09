---
title: Blazor Web App — Testing Plan
---

# Blazor Web App — Excruciatingly Detailed Testing Plan

This document provides an **excruciatingly detailed testing plan** for the **Blazor web application** (ProjectMcp.WebApp) described in [14 — Project Web App](14-project-web-app.html) and [22 — Blazor Web App Implementation](22-blazor-webapp-implementation.html). It covers unit, integration, end-to-end, and manual testing for auth, scope enforcement, tree, search, reports, Gantt, issues, and all entity CRUD. It aligns with [12 — Testing Plan](12-testing-plan.html), [06 — Tech Requirements](06-tech-requirements.html), and [03 — Data Model](03-data-model.html).

**References:** [14 — Project Web App](14-project-web-app.html), [22 — Blazor Web App Implementation](22-blazor-webapp-implementation.html), [12 — Testing Plan](12-testing-plan.html), [06 — Tech Requirements](06-tech-requirements.html), [03 — Data Model](03-data-model.html).

**Note:** **Work items and tasks are the same entity**; a **task** is a **work item with level = Task**. Test cases that mention tasks apply to work_items at level = Task.

---

## 1. Objectives and scope

### 1.1 Goals

- **Auth:** Every auth path (sign-in, callback, claims, sign-out) has at least one test; SUDO and scope claims are verified.
- **Scope:** Every data service and page that loads data is tested with in-scope and out-of-scope inputs; out-of-scope returns 403 or empty and is logged.
- **UI flows:** Critical paths (login → dashboard → tree → detail → CRUD, search, reports, Gantt, issues) have integration or E2E coverage.
- **No secrets:** No connection strings, GitHub secrets, or tokens in test code; use env or test-specific config and mocks where appropriate.

### 1.2 In scope

- Unit tests for **UserScopeService**, **scope enforcement** in services, **SUDO** check, **TreeService** (with mocked data layer), DTO validation, and (optional) Blazor component tests with bUnit or equivalent.
- Integration tests with **real PostgreSQL** (Testcontainers or CI service): **auth callback** (with mocked or test GitHub OAuth), **all scoped services** (enterprise, project, requirement, work item, task, milestone, release, issue, domain, system, asset, resource, keyword, search, reports), **TreeService** lazy load, **scope rejection** (403 and log), **CurrentResourceId** resolution (oauth2_sub).
- E2E tests (browser): **login flow**, **ScopeGuard** when no scope, **tree** expand and navigate, **detail pages** and **CRUD**, **search** (keyword and full-text), **reports**, **Gantt**, **issues** list and detail; **SUDO** create enterprise vs non-SUDO 403.
- **Manual checklist** for release: sign-in, scope, tree, CRUD, search, reports, Gantt, issues, dashboard, SUDO, logging.

### 1.3 Out of scope (v1)

- Mobile app testing (see [15 — Mobile App](15-mobile-app.html)).
- MCP server tool/resource testing (covered in [12 — Testing Plan](12-testing-plan.html)).
- Load or performance benchmarks (can be added later).

---

## 2. Unit tests

### 2.1 Scope and responsibilities

- **IUserScopeService:** Given a ClaimsPrincipal with allowed_enterprises and allowed_projects claims (or empty), returns UserScope with correct AllowedEnterpriseIds, AllowedProjectIds; when claims are empty, returns empty lists (and optionally loads from DB if implemented). When NameIdentifier is set and a mock IResourceService returns a resource, CurrentResourceId is set.
- **Scope enforcement in services:** For each service (e.g. IProjectService, IWorkItemService), given UserScope that does **not** include a given enterprise or project id, calling GetById(id) or Update(id, dto) for an entity in that enterprise/project must throw (e.g. InvalidOperationException or custom ScopeViolationException) or return null/403; no data leaked. Use **mocked** TodoEngine View or repositories and inject a test UserScope.
- **SUDO check:** A helper or service that checks "can create enterprise" must return true only when User has role SUDO; unit test with mock ClaimsPrincipal (with and without SUDO role).
- **DTO validation:** Create/Update DTOs (e.g. CreateProjectRequest, UpdateWorkItemRequest) validated with DataAnnotations or FluentValidation; unit tests for valid, missing required, and invalid enum/format.
- **TreeService (mocked):** GetChildren(Enterprise, enterpriseId, scope) returns only projects in AllowedProjectIds for that enterprise; GetChildren(Project, projectId, scope) returns only data when projectId is in AllowedProjectIds. Mock the underlying data layer; assert filtering.
- **Optional — Blazor components:** Use bUnit (or equivalent) to render ScopeGuard with no scope → "You have no projects assigned" visible; with scope → child content visible. Render breadcrumb with given path. Minimal component tests to avoid brittle UI tests.

### 2.2 Test project layout

```
tests/
  ProjectMcp.WebApp.Tests.Unit/
    Services/
      UserScopeServiceTests.cs
      ScopeEnforcementTests.cs
      SudoCheckTests.cs
    Tree/
      TreeServiceUnitTests.cs
    Validation/
      ProjectDtoValidationTests.cs
      WorkItemDtoValidationTests.cs
      RequirementDtoValidationTests.cs
      IssueDtoValidationTests.cs
    Components/          (optional)
      ScopeGuardTests.cs
      BreadcrumbTests.cs
```

### 2.3 Key unit test cases (table)

| Area | Test case | Expected |
|------|-----------|----------|
| **UserScopeService** | Claims with allowed_enterprises = [E1], allowed_projects = [P1] | UserScope.AllowedEnterpriseIds contains E1; AllowedProjectIds contains P1. |
| **UserScopeService** | Claims with empty allowed_enterprises | AllowedEnterpriseIds empty; AllowedProjectIds empty. |
| **UserScopeService** | NameIdentifier = "12345"; mock resource with oauth2_sub 12345 | CurrentResourceId = that resource's id. |
| **UserScopeService** | NameIdentifier set but no resource in DB (mock returns null) | CurrentResourceId = null. |
| **Scope enforcement** | IProjectService.GetById(projectId) with projectId not in UserScope.AllowedProjectIds | Throws or returns null; no exception with data leak. |
| **Scope enforcement** | IWorkItemService.List(scope) with scope.AllowedProjectIds = [P1] | Only work items for P1 returned (mock returns only those). |
| **SUDO** | User with role SUDO | CanCreateEnterprise returns true. |
| **SUDO** | User without role SUDO | CanCreateEnterprise returns false. |
| **TreeService** | GetChildren(Enterprise, E1, scope) where E1 not in AllowedEnterpriseIds | Returns empty or throws. |
| **TreeService** | GetChildren(Project, P1, scope) where P1 in AllowedProjectIds | Returns children (mocked); list not empty when mock has data. |
| **CreateProjectRequest** | Missing name | Validation error. |
| **CreateWorkItemRequest** | Level = Task, valid title | Valid. |
| **CreateWorkItemRequest** | Invalid level value | Validation error. |
| **ScopeGuard (bUnit)** | UserScope empty | Renders "You have no projects assigned"; no tree. |
| **ScopeGuard (bUnit)** | UserScope has enterprises | Renders child content (e.g. tree placeholder). |

### 2.4 Coverage expectations

- **Target:** ≥ 80% line coverage for **UserScopeService**, **scope enforcement** logic, **TreeService** filtering, **SUDO** and validation helpers. Exclude: Blazor framework glue, main Program.cs, static assets.
- **CI:** Fail build if coverage drops below threshold (e.g. 75%) for WebApp service and scope code.

---

## 3. Integration tests

### 3.1 Scope and responsibilities

- **Database:** Real PostgreSQL via Testcontainers or CI-provided service. Apply migrations (same as MCP/WebApp). Seed test data: at least two enterprises (E1, E2), two projects (P1 under E1, P2 under E2), one resource with oauth2_sub (e.g. "test-github-123"), and a mapping that grants that resource access to E1 and P1 only (so UserScope can be built from DB).
- **Auth callback:** Use a test server (WebApplicationFactory) or mock the GitHub OAuth middleware so that "callback" with a code results in a signed-in user with claims (allowed_enterprises, allowed_projects, SUDO or not). Assert redirect to dashboard or home and cookie set.
- **Services with real DB:** For each of IEnterpriseService, IProjectService, IRequirementService, IWorkItemService, IMilestoneService, IReleaseService, IIssueService, IDomainService, ISystemService, IAssetService, IResourceService, IKeywordService, ISearchService, IReportsService: run at least one **in-scope** test (e.g. List or GetById with scope that includes the entity) and one **out-of-scope** test (e.g. GetById for entity in other enterprise → 403 or null, and assert log or exception).
- **TreeService:** With real DB and seeded data, get UserScope for user with E1/P1; call GetChildren(Enterprise, E1Id, scope) → returns projects under E1 only; expand project P1 → GetChildren(Project, P1Id, scope) → returns requirements, work items, releases as applicable. Call with scope that does not include P1 → GetChildren(Project, P1Id, scope) returns empty or throws.
- **CurrentResourceId:** Seed resource with oauth2_sub = "test-github-123"; create ClaimsPrincipal with NameIdentifier = "test-github-123"; resolve UserScope → CurrentResourceId equals that resource's id. Seed no resource for that id → CurrentResourceId null.
- **SUDO:** Create enterprise via service or API with user that has SUDO role → 201/success. With user that does not have SUDO → 403 and no row in enterprise table.
- **Search (full-text):** If tsvector/GIN is in migrations, run ISearchService search with query string; assert results are filtered by UserScope (only entities in allowed enterprises/projects) and results grouped by entity type with snippet.
- **Reports:** IReportsService milestone progress for a milestone in scope → returns DTO with counts/percentages. For milestone not in scope → empty or 403.
- **Gantt data:** Service that returns work items/tasks with dates and dependencies for a project; call with project in scope → list; with project out of scope → empty or 403.

### 3.2 Test database setup

- **Option A:** Testcontainers — start Postgres container per test run or per class; run migrations (from TodoEngine or WebApp); seed via SQL or EF; run tests; dispose.
- **Option B:** CI Postgres service; run migrations once; use unique schema or database per run if parallel.
- **Connection string:** From env `TEST_DATABASE_URL` or Testcontainers-generated URL. No hardcoded credentials.
- **Seed data (minimal for WebApp):**
  - **enterprises:** E1, E2 (different ids).
  - **projects:** P1 (enterprise_id = E1), P2 (enterprise_id = E2).
  - **resources:** R1 (enterprise_id = E1, oauth2_sub = "test-github-123").
  - **Mapping (if used):** e.g. resource_enterprises (resource_id = R1, enterprise_id = E1), resource_projects (resource_id = R1, project_id = P1). Or derive from a single "user scope" table.
  - **work_items:** 1–2 tasks under P1 for list/detail tests.
  - **milestones:** 1 under E1; **releases:** 1 under P1.
  - **requirements:** 0–1 under P1.
  - **issues:** 0–1 under P1.

### 3.3 Test project layout

```
tests/
  ProjectMcp.WebApp.Tests.Integration/
    Auth/
      GitHubOAuthCallbackIntegrationTests.cs
      SignInSignOutIntegrationTests.cs
    Services/
      EnterpriseServiceScopeIntegrationTests.cs
      ProjectServiceScopeIntegrationTests.cs
      RequirementServiceScopeIntegrationTests.cs
      WorkItemServiceScopeIntegrationTests.cs
      MilestoneReleaseServiceScopeIntegrationTests.cs
      IssueServiceScopeIntegrationTests.cs
      DomainSystemAssetResourceKeywordScopeIntegrationTests.cs
      SearchServiceIntegrationTests.cs
      ReportsServiceIntegrationTests.cs
      GanttDataServiceIntegrationTests.cs
    Tree/
      TreeServiceIntegrationTests.cs
    Scope/
      UserScopeResolutionIntegrationTests.cs
      SudoCreateEnterpriseIntegrationTests.cs
      CrossEnterpriseRejectionIntegrationTests.cs
```

### 3.4 Key integration test cases (table)

| Area | Test case | Expected |
|------|-----------|----------|
| **Auth callback** | POST /signin-github with code (or mock); follow redirect | User signed in; cookie set; redirect to / or dashboard. |
| **Auth callback** | OnCreatingTicket adds allowed_enterprises, allowed_projects from DB | Claims in cookie contain E1, P1 for test user. |
| **UserScope** | Resolve with NameIdentifier = test-github-123, DB has resource R1 with oauth2_sub | CurrentResourceId = R1.Id. |
| **UserScope** | Resolve with no matching resource | CurrentResourceId = null. |
| **IProjectService** | List(scope with P1) | Returns project P1 only. |
| **IProjectService** | GetById(P2) with scope only E1/P1 | Returns null or throws; 403; no P2 data. |
| **IWorkItemService** | List(scope) for P1 | Returns work items for P1 only. |
| **TreeService** | GetChildren(Enterprise, E1, scope) | Returns projects under E1 (e.g. P1); not P2. |
| **TreeService** | GetChildren(Project, P2, scope) where scope has only P1 | Returns empty or throws. |
| **ISearchService** | FullTextSearch(query, scope) | Results only from enterprises/projects in scope. |
| **IReportsService** | MilestoneProgress(milestoneId, scope) in scope | DTO with progress data. |
| **IReportsService** | MilestoneProgress(milestoneId, scope) out of scope | Empty or 403. |
| **SUDO** | CreateEnterprise with user without SUDO | 403; enterprise not created. |
| **SUDO** | CreateEnterprise with user with SUDO | 201; enterprise row created. |
| **Cross-enterprise** | Any read/update for entity in E2 when scope is E1/P1 | 403 or empty; log entry with user id, endpoint, target enterprise. |

### 3.5 Isolation and cleanup

- Each test that mutates data uses a **transaction that is rolled back** or a **dedicated set of ids** (e.g. GUIDs generated per test) so tests do not interfere. Prefer rollback so seed data is unchanged for next test.
- **Auth tests:** Use in-memory or test cookie auth; do not call real GitHub. Mock or stub the OAuth handler so that "sign-in" with a test code creates a principal with desired claims.

---

## 4. End-to-end (E2E) tests

### 4.1 Scope and responsibilities

- **Browser automation:** Use **Playwright** (or Selenium) to drive a real browser against the running WebApp (or TestServer with WebApplicationFactory). Database: Testcontainers Postgres with migrations and seed (same as integration).
- **Flows:** (1) **Unauthenticated** → redirect to login. (2) **Login** (mock or test GitHub) → redirect to dashboard; header shows user. (3) **No scope** → ScopeGuard shows "You have no projects assigned"; tree and data not visible. (4) **With scope** → enterprise switcher shows E1; tree shows E1 and P1; click project → detail page; click task → task detail; create task, update, delete (with confirmation). (5) **Search** → type in search box; results grouped by type; click result → detail. (6) **Reports** → select enterprise/project; report shows data. (7) **Gantt** → select project; Gantt loads with tasks/dates. (8) **Issues** → list and detail; create issue. (9) **SUDO** → as SUDO user, create enterprise; as non-SUDO, attempt create enterprise → 403 or error message.
- **ScopeGuard:** E2E must assert that when the signed-in user has **no** allowed enterprises/projects, the page shows the exact message "You have no projects assigned" (or configured copy) and does not render tree or entity lists.

### 4.2 E2E test flow (example)

1. Start Testcontainers Postgres; run migrations; seed E1, P1, R1, mapping, one task, one milestone.
2. Start WebApp (WebApplicationFactory or hosted server) with test auth (e.g. mock GitHub to return user id and inject claims for E1/P1).
3. **Playwright:** Navigate to `/` → redirected to login (or GitHub). Complete login (or mock) → land on dashboard.
4. Assert dashboard shows "My tasks" or "Open issues" (or placeholder) when CurrentResourceId is set.
5. Navigate to tree; expand enterprise E1; assert project P1 appears; click P1 → project detail page; assert breadcrumb shows E1 / P1.
6. From project detail, open task list or work item; click task → task detail; assert title and status.
7. Create new task (title "E2E task"); assert it appears in list; update status to done; delete with confirmation; assert gone.
8. Use global search; type a known string from seeded task; assert result appears and is in scope.
9. Open Reports; select E1/P1; assert milestone progress or project progress shows data.
10. Open Gantt; select P1; assert at least one bar or task row.
11. Open Issues; create issue; assert in list; open detail.
12. **SUDO:** Sign in as non-SUDO user; attempt create enterprise (e.g. via UI or API); assert 403 or error. Sign in as SUDO; create enterprise; assert success and new enterprise in tree or list.
13. **No scope:** Sign in user with **empty** allowed_enterprises and allowed_projects; navigate to `/`; assert ScopeGuard message "You have no projects assigned"; assert no tree and no data panels.

### 4.3 E2E implementation options

| Option | Pros | Cons |
|--------|------|------|
| **WebApplicationFactory** | Same process; fast; no real browser | Does not test real browser/JS; some Blazor Server behavior may differ. |
| **Playwright + hosted server** | Real browser; real clicks and navigation | Slower; need to start server and DB; flakier if timing issues. |

**Recommendation:** Use **Playwright** (or Selenium) with a **hosted** WebApp (e.g. `dotnet run` or WebApplicationFactory with browser) so that Blazor Server and auth cookies are exercised. Run E2E in CI on main/PR or nightly; optional for every commit.

### 4.4 E2E test layout and CI

- **Location:** `tests/ProjectMcp.WebApp.Tests.E2E/` with:
  - `LoginFlowTests.cs`
  - `ScopeGuardTests.cs`
  - `TreeAndDetailTests.cs`
  - `CrudTaskTests.cs`
  - `SearchTests.cs`
  - `ReportsTests.cs`
  - `GanttTests.cs`
  - `IssueTests.cs`
  - `SudoCreateEnterpriseTests.cs`
- **CI:** Run when DB and WebApp can be started (Testcontainers + WebApplicationFactory or separate job). **Timeout:** Global suite timeout (e.g. 5–10 minutes) and per-test timeout (e.g. 60–90 seconds).
- **Test auth:** Use a test authentication scheme or mock GitHub so no real GitHub credentials are needed in CI.

---

## 5. Manual testing

### 5.1 Manual test checklist (Blazor Web App)

Use this after deployment or before release of the web app.

| # | Area | Steps | Pass criteria |
|---|------|--------|----------------|
| 1 | **Sign-in** | Open app URL; click sign-in with GitHub | Redirects to GitHub; after auth, back to app with user name in header. |
| 2 | **No scope** | Sign in as user with no enterprises/projects assigned | "You have no projects assigned" shown; no tree; no data. |
| 3 | **Enterprise switcher** | Sign in with scope; check header | Enterprise dropdown shows only allowed enterprises; switching updates tree root. |
| 4 | **Tree** | Expand enterprise → projects; expand project → requirements, work, releases | Children load; counts/badges if implemented; click node opens detail. |
| 5 | **Breadcrumb** | From tree, open project then requirement or task | Breadcrumb shows enterprise → project → entity. |
| 6 | **Project detail** | Open project; check name, description, status, tech stack; tabs/sections | Data matches DB; requirements, work, releases listed. |
| 7 | **Task create** | Create task with title; save | Task appears in list and in project detail. |
| 8 | **Task update/delete** | Update task status; delete task with confirmation | List and detail reflect changes; deleted task gone. |
| 9 | **Work item** | Create work item (level Work); add child task | Hierarchy and work queue (if shown) correct. |
| 10 | **Search** | Type in global search; apply keyword filter | Results grouped by type; only scoped data; snippets visible. |
| 11 | **Reports** | Open Reports; select enterprise, project, milestone | Milestone/project/resource reports show data; charts or tables render. |
| 12 | **Gantt** | Open Gantt; select project; set time range | Tasks/work items with dates and dependency arrows; milestone/release markers if implemented. |
| 13 | **Issues** | List issues; create issue; link to requirement/work item; open from requirement detail | Issues section shows linked issues; state and assignee correct. |
| 14 | **Dashboard** | After login, view dashboard | My tasks, open issues, milestone deadlines (or placeholders) visible when scope and resource set. |
| 15 | **SUDO** | As non-SUDO user, try create enterprise | 403 or error message; enterprise not created. |
| 16 | **SUDO** | As SUDO user, create enterprise | Success; new enterprise in tree/switcher. |
| 17 | **Cross-enterprise** | (If possible) Attempt to open URL for entity in another enterprise (e.g. /project/{P2}) | 403 or 404; no data leaked; log entry for follow-up. |
| 18 | **Logging** | Perform sign-in, CRUD, and one out-of-scope attempt; check logs | Auth events, user/resource id, and scope violation logged; no secrets in logs. |

### 5.2 Exploratory testing

- Large tree (many projects, many tasks); pagination or "load more" on task list.
- Invalid form input (empty title, invalid enum); assert validation messages.
- Sign-out and sign-in again; assert scope and data reload correctly.

---

## 6. Test data and fixtures

### 6.1 Seed data (integration and E2E)

- **Enterprises:** E1, E2 (names "Test Enterprise 1", "Test Enterprise 2").
- **Projects:** P1 (E1, "Test Project 1", active), P2 (E2, "Test Project 2", active).
- **Resources:** R1 (E1, "Test User", oauth2_sub = "test-github-123"); optionally R2 (E2) for cross-enterprise tests.
- **Scope mapping:** Grant R1 access to E1 and P1 only (via resource_enterprises, resource_projects, or app-specific table).
- **Work items:** Task T1 under P1 (title "Seed task", status todo); optional work item W1 with level = Work and child task.
- **Milestones:** M1 under E1; **Releases:** R1 under P1 (name "v0.1").
- **Requirements:** REQ1 under P1 (optional).
- **Issues:** ISS1 under P1 (optional, state Open).
- **Keywords:** K1 under E1; entity_keywords linking P1 or T1 to K1 for keyword search tests.

### 6.2 Fixtures and builders

- **UserScopeFixture:** Build UserScope with given AllowedEnterpriseIds, AllowedProjectIds, CurrentResourceId (or null).
- **ClaimsPrincipalFixture:** Build principal with NameIdentifier, role SUDO or not, allowed_enterprises, allowed_projects (as claims).
- **DTO builders:** CreateProjectRequest, CreateWorkItemRequest, CreateTaskRequest, etc., with valid defaults for happy-path tests.

### 6.3 No production data

- All test data is synthetic; use unique ids (e.g. GUIDs) or a dedicated test schema. No production connection strings or user accounts in tests.

---

## 7. Environments and configuration

| Environment | Purpose | Database | Auth (GitHub) |
|-------------|---------|----------|----------------|
| **Local dev** | Developer runs WebApp + tests | Local Postgres or Testcontainers | Test OAuth app or mock |
| **CI** | Unit + integration + E2E | Testcontainers or CI Postgres | Mock or test GitHub OAuth; no real client secret |
| **Staging** | Manual and E2E before release | Dedicated staging DB | Staging GitHub OAuth app |
| **Production** | Live use | Production DB | Production GitHub OAuth app |

- **Secrets:** Never commit `GITHUB_CLIENT_SECRET` or DB connection strings. CI uses env or secret store; tests use Testcontainers URL or `TEST_DATABASE_URL`.
- **Test env vars:** Document `TEST_DATABASE_URL`, optional `TEST_GITHUB_CLIENT_ID` (for test OAuth app), and any mock flags (e.g. `USE_MOCK_GITHUB=true`) in README or CI workflow.

---

## 8. Quality gates and CI

| Gate | When | Action |
|------|------|--------|
| **WebApp unit tests** | Every commit / PR | Must pass; coverage ≥ threshold for UserScope, scope enforcement, TreeService, validation. |
| **WebApp integration tests** | Every commit / PR | Must pass; DB from Testcontainers or CI Postgres; auth mocked. |
| **WebApp E2E tests** | PR to main or nightly | Must pass; optional for every commit if slow. |
| **Linting / format** | Every commit | Enforce editorconfig or dotnet format for WebApp and tests. |
| **No secrets** | Every commit | Scan for accidental secrets in WebApp and test projects. |
| **Manual checklist** | Before release of WebApp | Complete Blazor manual checklist (§5.1) and log results. |

---

## 9. Summary table: what to test where (Blazor Web App)

| Area | Unit | Integration | E2E | Manual |
|------|------|-------------|-----|--------|
| **UserScopeService** | Claims → scope; CurrentResourceId from oauth2_sub | Resolve with real DB and resource | — | — |
| **Scope enforcement** | Each service rejects out-of-scope (mock) | Each service with real DB; 403 or empty for other enterprise | — | Cross-enterprise attempt |
| **SUDO** | CanCreateEnterprise true/false by role | Create enterprise 403 vs 201 with real DB | SUDO create vs non-SUDO 403 | Checklist |
| **Auth (sign-in, callback)** | — | Callback adds claims; redirect; cookie | Login flow; redirect | Sign-in |
| **ScopeGuard** | bUnit: no scope → message | — | No scope → message; no tree | No scope |
| **Tree** | TreeService filter (mock) | GetChildren with real DB; scope filter | Expand and navigate; detail | Tree + breadcrumb |
| **Project / Task / Work item CRUD** | DTO validation | Service CRUD with scope | Create, update, delete task | CRUD steps |
| **Search** | — | ISearchService FTS + scope | Search box; results | Search |
| **Reports** | — | IReportsService with scope | Reports page; selectors | Reports |
| **Gantt** | — | Gantt data service with scope | Gantt page; bars/dependencies | Gantt |
| **Issues** | DTO validation | IIssueService with scope | List, create, detail | Issues |
| **Dashboard** | — | — | My tasks, issues, milestones | Dashboard |
| **Logging** | — | Assert log properties on scope violation | — | Logging check |

---

## 10. Dependencies on other docs

- **Test data shape** aligns with [03 — Data Model](03-data-model.html) (entities, ids, slugs).
- **Scope and SUDO** behavior align with [14 — Project Web App](14-project-web-app.html) and [22 — Blazor Web App Implementation](22-blazor-webapp-implementation.html).
- **Logging** requirements (session, resource, correlation id; exception Data and inner exceptions) from [06 — Tech Requirements](06-tech-requirements.html).
- **Overall** testing pyramid and CI approach from [12 — Testing Plan](12-testing-plan.html); this document is the **Blazor-specific** extension of that plan.

This plan ensures excruciatingly detailed coverage of authentication, scope enforcement, tree, search, reports, Gantt, issues, dashboard, and SUDO for the Blazor web app, with clear unit, integration, E2E, and manual test cases and a defined test project layout and quality gates.

## Appendix A - Former gap analysis (merged)

This appendix merges the previously separate gap analysis into the testing plan so the gaps and their remediation are preserved in one place.

| Gap ID | Area | Former severity | Gap summary | Remediation in this plan |
|--------|------|-----------------|-------------|---------------------------|
| GAP-004 | Testing plan coverage | Medium | Dedicated web app testing plan missing | This document provides the full plan (Sections 1-11). |
| GAP-005 | Tooling & project layout | Medium | UI/component test tooling not specified | Tooling and layout specified in Sections 2-3. |
| GAP-006 | OAuth2 auth flow | Medium | Callback, claim mapping, SUDO tests undefined | Auth tests defined in Sections 4-5. |
| GAP-007 | Scope enforcement | High | Claims-based scope tests missing | Scope tests defined in Sections 4-5 and 6. |
| GAP-008 | UI/CRUD behavior | Medium | Tree and CRUD tests missing | UI/CRUD tests defined in Sections 4-5. |
| GAP-009 | Search/Reports/Gantt | Medium | Coverage missing | Tests defined in Sections 4-5 and 6. |
| GAP-010 | E2E automation | Medium | Browser automation strategy missing | Playwright plan defined in Section 6 and 10. |
| GAP-011 | Non-functional | Medium | a11y/cross-browser/perf missing | Non-functional tests defined in Section 8. |
| GAP-012 | Test data & env | Medium | Seed data and OAuth test env missing | Defined in Section 9. |
| GAP-013 | Security testing | High | CSRF/XSS/authorization tests missing | Security tests defined in Section 7. |
