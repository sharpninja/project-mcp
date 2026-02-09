---
title: Testing Plan
---

# Testing Plan

This document defines a **detailed testing plan** for the Software Project Management MCP server and the **TODO engine library** (ProjectMcp.TodoEngine). It covers unit, integration, end-to-end, and manual testing; test data; environments; and quality gates. It aligns with [04 — MCP Surface](04-mcp-surface.html), [10 — MCP Endpoint Diagrams](10-mcp-endpoint-diagrams.html), [06 — Tech Requirements](06-tech-requirements.html), and [19 — TODO Library Implementation](19-todo-library-implementation.html).

**Note:** **Work items and tasks are the same entity**; a **task** is a **work item with level = Task**. Test cases that mention tasks apply to work_items at level = Task.

---

## 1. Testing pyramid and objectives

| Level | Purpose | Scope | Tools / approach |
|-------|---------|--------|-------------------|
| **Unit** | Logic in isolation; fast feedback | Handlers, validators, path resolution, DTOs | xUnit, NSubstitute/FakeItEasy |
| **Integration** | Server + database; real I/O | Repositories, DbContext, migrations | xUnit, Testcontainers (PostgreSQL) or dedicated test DB |
| **E2E** | Full MCP flow (stdio or REST) | Initialize, context_key, tools, resources | Script or test client over stdio or HTTP REST; real or test DB |
| **Cursor agent CLI script** | Agent-driven workflow + DB assertions | Locally running ProjectMCP; scripted prompts; DB state per step | Cursor agent CLI; prompt script; DB snapshot comparison |
| **Manual** | UX and host integration | Cursor (or other MCP client) | Checklist; exploratory |

**Objectives:**

- All tool and resource endpoints have at least one happy-path test (integration or E2E).
- The **TODO engine library** (View, SlugService, scope enforcement, AuditContext) has unit tests (with mocks) and integration tests (with real DB) in isolation; see §2.3, §3.5.
- Path safety and input validation have dedicated unit tests where applicable.
- No secrets in tests; use env or test-specific config for DB.
- CI runs unit and integration tests on every commit; E2E optionally on schedule or tag.

---

## 2. Unit tests

### 2.1 Scope and responsibilities

- **Handlers / service logic:** Given valid/invalid inputs, assert returned result or thrown validation.
- **Path resolution (if any file-path tool exists):** Resolve path relative to root; reject `..`, absolute paths outside root, null/empty.
- **DTO / request parsing:** Deserialize tool arguments; validate required fields and types.
- **Slug/ID resolution:** If implemented in a dedicated service, test slug → GUID resolution and invalid slug handling.
- **TODO library — SlugService (unit):** Allocate slug format (e.g. WI000001, E1-P001-REQ0001), next index per owner/entity type, invalid owner or entity type.
- **TODO library — View (unit, mocked repos):** View methods with valid scope return expected DTOs; with scope for different enterprise, throw or return scope violation (ScopeViolationException or equivalent); GetProject(scope) returns null when project not in scope.
- **TODO library — DI:** After AddTodoEngine(services, options), resolve ITodoView, ISlugService, and key repositories from ServiceProvider; ensure no missing registration.

### 2.2 Test project layout

```
tests/
  ProjectMcp.Tests.Unit/
    Handlers/
      ScopeSetHandlerTests.cs
      EnterpriseHandlerTests.cs
      ProjectGetInfoHandlerTests.cs
      ProjectUpdateHandlerTests.cs
      RequirementHandlerTests.cs
      StandardHandlerTests.cs
      WorkItemHandlerTests.cs
      TaskCreateHandlerTests.cs
      TaskUpdateHandlerTests.cs
      TaskListHandlerTests.cs
      TaskDeleteHandlerTests.cs
      IssueHandlerTests.cs
      MilestoneHandlerTests.cs
      ReleaseHandlerTests.cs
      DomainHandlerTests.cs
      SystemHandlerTests.cs
      AssetHandlerTests.cs
      ResourceHandlerTests.cs
      KeywordHandlerTests.cs
      WorkQueueHandlerTests.cs
      AssociationHandlerTests.cs
      DocRegisterHandlerTests.cs
      DocListHandlerTests.cs
    Services/
      PathResolverTests.cs
      SessionStoreTests.cs
    TodoEngine/
      SlugServiceTests.cs
      TodoViewUnitTests.cs
      TodoEngineDiTests.cs
    Validation/
      ProjectUpdateValidatorTests.cs
      RequirementCreateValidatorTests.cs
      WorkItemCreateValidatorTests.cs
      TaskCreateValidatorTests.cs
```

### 2.3 Key unit test cases

| Area | Test case | Expected |
|------|-----------|----------|
| **Path resolution** | Path "spec/readme.md" with root "/repo" | Resolves to "/repo/spec/readme.md" (or equivalent), allowed. |
| **Path resolution** | Path "../etc/passwd" | Rejected (outside root). |
| **Path resolution** | Path "/absolute/outside/root" | Rejected. |
| **Path resolution** | Null or empty path | Rejected. |
| **scope_set** | Valid scope_slug | Returns scope; session updated (mock session store). |
| **scope_set** | Missing scope_slug | Validation error. |
| **scope_set** | Invalid slug format | Validation error. |
| **requirement_create** | Missing title | Validation error. |
| **work_item_create** | Invalid level value | Validation error. |
| **task_create** | Missing title | Validation error. |
| **task_create** | Valid title, optional fields null | Success; created DTO returned (mocked store). |
| **task_update** | Missing id | Validation error. |
| **task_list** | Filters applied | Correct filter object passed to store (mock). |
| **project_update** | Invalid status value | Validation error. |
| **project_update** | Valid techStack and docs | Parsed and passed to store. |
| **SlugService** | AllocateSlugAsync(WorkItem, "E1-P001") | Returns slug with prefix and next index (e.g. E1-P001-WI000001). |
| **SlugService** | AllocateSlugAsync for same owner twice | Second slug has incremented index. |
| **SlugService** | Invalid entity type or empty owner | Throws or returns error. |
| **TodoView (mocked)** | GetProject(scope) with project in scope | Returns project DTO. |
| **TodoView (mocked)** | GetProject(scope) with project in other enterprise | Returns null or throws ScopeViolationException. |
| **TodoView (mocked)** | CreateWorkItem(scope, dto) with projectId not in scope | Throws or returns scope violation. |
| **AddTodoEngine** | Resolve ITodoView, ISlugService, IProjectRepository | All resolve successfully. |

### 2.4 Tool response shape

- Unit tests that assert tool **response shape** (e.g. JSON with expected keys, or error with `error` and `isError: true`) without hitting the database.

### 2.5 Coverage expectations

- **Target:** ≥ 80% line coverage for handler/service and path-resolution code, and for **TODO engine library** code (View, SlugService, scope validation).
- **Exclude:** MCP SDK glue, main entry point, trivial DTOs, and generated/migration code.
- **CI:** Fail the build if coverage drops below a threshold (e.g. 75%) or if new code in covered areas has no tests.

---

## 3. Integration tests

### 3.1 Scope and responsibilities

- **Database:** Real PostgreSQL (Testcontainers or CI-provided service). Apply migrations before tests; optionally seed minimal data.
- **Repositories / DbContext:** Create enterprise, project, requirement, standard, work item, task, issue, domain, system, asset, resource, keyword, milestone, release; read and update; delete. Assert persistence and scope (e.g. task list filtered by project_id).
- **Session store:** If backed by DB or shared state, test context_key → scope resolution and scope_set/scope_get across "requests."
- **Full tool flow with DB:** Call handler with valid scope and params; handler uses real repository; assert DB state and response (e.g. work_item_create inserts row, work_item_list returns it).

### 3.2 Test database setup

- **Option A:** Testcontainers — start Postgres container per test run or per class; run migrations; run tests; dispose.
- **Option B:** CI service (e.g. GitHub Actions Postgres service); run migrations once; use unique schema or DB per run if parallel.
- **Connection string:** From env (e.g. `TEST_DATABASE_URL`) or Testcontainers-generated URL. No hardcoded credentials.

### 3.3 Test project layout

```
tests/
  ProjectMcp.Tests.Integration/
    Data/
      EnterpriseRepositoryTests.cs
      ProjectRepositoryTests.cs
      RequirementRepositoryTests.cs
      StandardRepositoryTests.cs
      WorkItemRepositoryTests.cs
      TaskRepositoryTests.cs
      IssueRepositoryTests.cs
      MilestoneRepositoryTests.cs
      ReleaseRepositoryTests.cs
      DomainRepositoryTests.cs
      SystemRepositoryTests.cs
      AssetRepositoryTests.cs
      ResourceRepositoryTests.cs
      KeywordRepositoryTests.cs
      AssociationRepositoryTests.cs
      DocRepositoryTests.cs
    HandlersWithDb/
      ScopeIntegrationTests.cs
      EnterpriseToolsIntegrationTests.cs
      ProjectToolsIntegrationTests.cs
      RequirementToolsIntegrationTests.cs
      StandardToolsIntegrationTests.cs
      WorkItemToolsIntegrationTests.cs
      TaskToolsIntegrationTests.cs
      IssueToolsIntegrationTests.cs
      PlanningToolsIntegrationTests.cs
      DomainToolsIntegrationTests.cs
      SystemToolsIntegrationTests.cs
      AssetToolsIntegrationTests.cs
      ResourceToolsIntegrationTests.cs
      KeywordToolsIntegrationTests.cs
      WorkQueueIntegrationTests.cs
      AssociationToolsIntegrationTests.cs
      DocToolsIntegrationTests.cs
    Resources/
      ResourceSpecIntegrationTests.cs
      ResourceTasksIntegrationTests.cs
      ResourcePlanIntegrationTests.cs
    TodoEngine/
      TodoViewIntegrationTests.cs
      SlugServiceIntegrationTests.cs
      TodoEngineScopeAndAuditTests.cs
```

### 3.4 Key integration test cases

| Area | Test case | Expected |
|------|-----------|----------|
| **Project** | Create project via repository; get by id | Project persisted; get returns same data. |
| **Project** | project_update creates then updates | One row; updated fields in DB and in response. |
| **Requirements** | requirement_create then requirement_list | List contains created requirement. |
| **Work items** | work_item_create then work_item_list | List contains created item. |
| **Tasks** | task_create then task_list with no filter | List contains created task. |
| **Tasks** | task_list with status filter | Only tasks with that status returned. |
| **Tasks** | task_update then task_list | Updated fields reflected. |
| **Tasks** | task_delete then task_list | Task no longer in list; get returns not found. |
| **Issues** | issue_create then issue_list | List contains created issue. |
| **Milestones** | milestone_create/list/update | CRUD with enterprise scope. |
| **Releases** | release_create/list/update | CRUD with project scope. |
| **Domains** | domain_create/list/update | CRUD with enterprise scope. |
| **Systems** | system_create/list/update | CRUD with enterprise scope. |
| **Assets** | asset_create/list/update | CRUD with enterprise scope. |
| **Resources** | resource_create/list/update | CRUD with enterprise scope. |
| **Keywords** | keyword_create/list/update | CRUD with enterprise scope. |
| **Associations** | item_dependency_add/remove | Dependency created and removed. |
| **Scope** | scope_set with valid scope_slug; then project_get_info | Returns project for that scope. |
| **Scope** | scope_set with invalid scope_slug | Error; no project returned on subsequent get. |
| **Scope** | Access project in another enterprise | 403 error; attempt logged. |
| **Resources** | Request project://current/spec after scope_set | Content matches project in DB for that scope. |
| **Resources** | Request project://current/tasks | Task list matches project_id from scope. |
| **Audit** | Change tracking records include session_id/resource_id/correlation_id | Audit rows include required fields. |

### 3.5 TODO engine library (integration, in isolation)

The **TODO engine library** ([19 — TODO Library Implementation](19-todo-library-implementation.html)) is testable in isolation with a real database (Testcontainers Postgres or in-memory SQLite). These tests verify the View, SlugService, repositories, scope enforcement, and audit context propagation **without** the MCP host.

| Area | Test case | Expected |
|------|-----------|----------|
| **View + DB** | Create project via View; GetProject(scope) | Project persisted; GetProject returns DTO for same scope. |
| **View + DB** | CreateWorkItem(scope, dto) then ListWorkItems(scope) | Work item in list; slug allocated (e.g. E1-P001-WI000001). |
| **View + DB** | CreateWorkItem for project in **another** enterprise (scope mismatch) | ScopeViolationException or equivalent; no row inserted. |
| **View + DB** | GetProject(scope) with scope.EnterpriseId different from project's | Returns null or throws; no data leaked. |
| **SlugService + DB** | AllocateSlugAsync(WorkItem, "E1-P001") with empty project | Returns E1-P001-WI000001 (or first index per [08 — Identifiers](08-identifiers.html)). |
| **SlugService + DB** | Two work items under same project | Second slug has incremented index (e.g. WI000002). |
| **Slug propagation** | Update entity display_id (e.g. move requirement); verify descendants | All descendant slugs updated in same transaction; or test service that performs propagation. |
| **AuditContext** | CreateWorkItem(scope, dto, auditContext); verify IAuditWriter or audit table | Audit record includes session_id, resource_id, correlation_id from AuditContext. |
| **AuditContext** | UpdateWorkItem with AuditContext | Update recorded with same audit fields. |
| **Provider** | Run same View + SlugService tests with SQLite (in-memory) | Behavior consistent; slug format and scope rules unchanged. |

**Test project layout:** Either a dedicated `ProjectMcp.TodoEngine.Tests` project or a folder under integration tests, e.g. `tests/ProjectMcp.Tests.Integration/TodoEngine/` with `TodoViewIntegrationTests.cs`, `SlugServiceIntegrationTests.cs`, `TodoEngineScopeAndAuditTests.cs`.

### 3.6 Isolation and cleanup

- Each test that mutates data should use a dedicated project/enterprise (e.g. unique GUID or slug) or run in a transaction that is rolled back so tests do not affect each other.
- Avoid shared mutable state; prefer fresh DB or transaction rollback per test.
- **TODO library tests:** Use a fresh DbContext (or new container) per test class or test method so slug allocation and scope tests do not interfere.

---

## 4. End-to-end (E2E) tests

### 4.1 Scope and responsibilities

- **Full MCP protocol (stdio or REST):** Start server process; send Initialize request (stdio or `POST /mcp/initialize`); receive response with context_key and tool list. Send tool calls (with context_key via envelope or `X-Context-Key` header); assert response content. Optionally request resources. Both transports should be covered (e.g. E2E for stdio, E2E for REST).
- **Real server process:** Either in-process server with stdio pipes or subprocess (e.g. `dotnet run`); for REST, run HTTP host and send HTTP requests. Database can be Testcontainers or dedicated E2E DB.

### 4.2 E2E test flow (example)

1. Start Postgres (Testcontainers or env); run migrations; optionally seed one enterprise and one project.
2. Start MCP server process with connection string and default scope (or no default).
3. Send Initialize (client name, version, capabilities).
4. Parse Initialize response; extract context_key.
5. Send scope_set(scope_slug) with context_key; assert success and scope in response.
6. Send project_get_info with context_key; assert project metadata (or empty).
7. Send project_update with context_key; assert success.
8. Send project_get_info again; assert updated data.
9. Send task_create(title: "E2E task") with context_key; assert task in response.
10. Send project://current/tasks resource read with context_key; assert task list contains the created task.
11. Send task_list with context_key; assert list contains the task.
12. Send task_update(id, status: "done"); then task_list; assert status updated.
13. Send task_delete(id); then task_list; assert task gone.
14. (Optional) Send requirement_create, work_item_create, issue_create, and domain_create; assert list results.
15. (Optional) Send milestone_create, release_create; assert success.
16. (Optional) Attempt out-of-scope read/write (different enterprise); assert 403 + log entry.
17. Shut down server process.

### 4.3 E2E implementation options

| Option | Pros | Cons |
|--------|------|------|
| **Subprocess** | Real deployment path | Slower; need to parse stdio. |
| **In-process with stdio pipes** | Fast; same process | May hide process-start issues. |
| **MCP client library** | Correct request/response format | Extra dependency; may need client SDK in test. |

Recommendation: Start with subprocess and simple JSON over stdin/stdout (or use MCP client if available for .NET) so E2E truly validates the full stack.

### 4.4 E2E test layout and CI

- **Location:** e.g. `tests/ProjectMcp.Tests.E2E/` or a dedicated E2E solution/folder.
- **CI:** Run on main/PR with Testcontainers Postgres; optionally run on schedule or only on release tag to keep PR feedback fast.
- **Timeout:** E2E suite should have a global timeout (e.g. 2–5 minutes) and per-test timeouts to avoid hangs.

---

## 5. Cursor agent CLI script-driven integration test

This test validates that a **real agent** (Cursor agent CLI), driving a **locally running ProjectMCP** instance, can "build this project in the MCP" by following a **script of prompts**. After **each prompt**, the test **compares the database state to an expected state** before submitting the next prompt. It ensures the agent’s use of tools and resources produces the intended persisted state at every step.

### 5.1 Purpose and scope

- **Purpose:** Confirm that the ProjectMCP server behaves correctly when consumed by the Cursor agent CLI: the agent can set scope, create/update project and tasks, milestones, releases, and docs in response to natural-language prompts, and the resulting database state matches expectations at each step.
- **Scope:** Integration test (real agent, real local server, real database). Requires Cursor agent CLI to be available and a script that submits prompts and then runs DB assertions.
 - **Skip condition:** If the Cursor agent CLI is not installed or not on PATH, **skip the test** and report the reason.

### 5.2 Prerequisites

| Prerequisite | Description |
|--------------|-------------|
| **Cursor agent CLI** | Cursor’s agent CLI (or equivalent) installed and on PATH, configured to use a locally running ProjectMCP server instance. |
| **ProjectMCP running locally** | ProjectMCP server started with a dedicated test database (e.g. Testcontainers or a CI/local Postgres). Server must be reachable by the agent (stdio if the CLI spawns the server, or network endpoint if the CLI connects to a pre-started server). |
| **Test database** | Empty or known initial state (migrations applied; no or minimal seed). Connection string known to both the server and the test harness for DB assertions. |
| **Repo under test** | The “this project” to build in the MCP: the ProjectMcp repo (or a fixture repo) so prompts can reference “this project” (e.g. set up project, add tasks for implementation plan, etc.). |

### 5.3 Test flow

1. **Setup**
   - Start or attach to a Postgres instance (e.g. Testcontainers); apply migrations; optionally seed one enterprise (and optionally one project) so the agent can set scope.
   - Start the ProjectMCP server process pointing at this database (or ensure the Cursor agent CLI is configured to start/use this server).
   - Load the **prompt script** (see below).
   - Record **initial DB state** (e.g. row counts for project, task, milestone, release; or full snapshot of relevant tables).

2. **For each step in the script (in order):**
   - **Submit prompt:** Send the next prompt from the script to the Cursor agent CLI (e.g. via CLI stdin or a driver that invokes the CLI with the prompt).
   - **Wait for completion:** Wait until the agent has finished (CLI exits or signals completion). Use a timeout to avoid hangs.
   - **Capture DB state:** Query the database for the state relevant to the test (e.g. all projects for the test enterprise, all tasks for the test project, milestones, releases).
   - **Compare to expected state:** Compare the captured state to the **expected state for this step** (defined in the script or a companion file). Comparison may be:
     - Row counts and key fields (e.g. one project with name "ProjectMcp", two tasks with titles matching expected).
     - Or a full snapshot (e.g. JSON export of project/tasks/milestones/releases) diffed against a golden file.
   - **Pass/fail:** If the state does not match the expected state, **fail the test** and optionally capture logs and DB dump for debugging. Do **not** continue to the next prompt.
   - If the state matches, continue to the next step.

3. **Teardown**
   - Stop the server (if started by the test).
   - Optionally tear down the database (Testcontainers dispose) or leave it for artifact collection on failure.

### 5.4 Prompt script: “Build this project in the MCP”

The script is a **ordered list of prompts** (and, per step, the **expected DB state** after the agent has acted on that prompt). The goal of the scenario is to have the agent “build this project in the MCP”—i.e. create and update project metadata, tasks, and planning in ProjectMCP to reflect the ProjectMcp repo (or a minimal version of it).

Example structure (prompts and expectations are illustrative):

| Step | Prompt (to Cursor agent CLI) | Expected DB state after step |
|------|------------------------------|------------------------------|
| 1 | “Set scope to enterprise … and project … (or create a project named ProjectMcp for this repo).” | Exactly one project; project name matches (e.g. "ProjectMcp"); scope can be used for subsequent steps. |
| 2 | “Update the project with description and tech stack: .NET 10, C#, PostgreSQL, MCP.” | project row: description and tech_stack (or equivalent) populated as specified. |
| 3 | “Add a task: ‘Scaffold MCP server and one tool’ with status todo.” | Exactly one task; title and status match. |
| 4 | “Add a task: ‘Implement data layer and migrations’.” | Exactly two tasks; second task title matches. |
| 5 | “List all tasks and then mark the first task as in-progress.” | Two tasks; first task status = in-progress. |
| 6 | “Add a milestone ‘v0.1’ and a release ‘0.1.0’ for this project.” | One milestone and one release; names/identifiers match. |

The actual **prompt script** and **expected state** (e.g. JSON or table) should live in the repo (e.g. `tests/ProjectMcp.Tests.Integration/CursorAgentScript/` or similar) so they can be versioned and updated when the scenario or schema changes.

### 5.5 Expected state format and comparison

- **Format:** Expected state can be defined as:
  - **Structured file (recommended):** e.g. JSON or YAML per step: `step-01-expected.json`, … listing expected entities (projects, tasks, milestones, releases) with key fields (id optional, name, title, status, etc.). The test loads this file and compares to the DB snapshot (normalizing order if needed, e.g. sort by id or title).
  - **Inline in script:** Script file has a section per step: prompt text plus expected state (table or mini-DSL).
- **Comparison:** Compare only the fields that the agent is expected to have set (e.g. name, title, status, count). Ignore generated ids and timestamps unless they are needed for ordering. Use tolerant comparison (e.g. trim strings, ignore irrelevant tables).

### 5.6 Implementation options

| Approach | Description | Pros | Cons |
|----------|-------------|------|------|
| **CLI subprocess** | Test harness spawns Cursor agent CLI as subprocess; passes prompt via stdin or args; waits for exit; then queries DB. | Real agent; no mock. | Requires CLI installed; slower; need to parse CLI output for errors. |
| **Script runner + driver** | Separate script (e.g. shell or Python) that runs the CLI for each prompt, then runs a DB assertion script (e.g. psql or a small .NET tool) that compares state to expected JSON. Test harness invokes the script runner and checks exit code. | Clear separation; script can be run manually. | Two pieces to maintain (prompts + assertion tool). |
| **CI job** | Dedicated CI job that starts ProjectMCP + DB, runs the prompt script (e.g. via Cursor CLI), then runs DB assertions. | Fits CI; can run on schedule or on tag. | CI must have Cursor agent CLI available (or skip if not present). |

Recommendation: Implement a **script runner** (e.g. in the test project or a `scripts/` directory) that: (1) ensures the server and DB are up, (2) runs the Cursor agent CLI for each prompt in order, (3) after each step runs a **DB assertion** (e.g. a small tool or script that queries the DB and diffs to expected state), (4) exits with non-zero if any step’s state does not match. The main test (xUnit or similar) can then invoke this runner and assert exit code zero, or the runner can be the test itself in a separate step in CI.

### 5.7 Test layout and CI

- **Location:** e.g. `tests/ProjectMcp.Tests.Integration/CursorAgentScript/` with:
  - `prompts.yaml` or `prompts.json` — ordered list of { prompt, expectedStateFile }.
  - `expected/step-01-expected.json`, etc.
  - Runner: `RunCursorAgentScript.cs` (or a shell script `run-cursor-agent-test.sh`) that executes the flow above.
- **CI:** Run only when Cursor agent CLI is available (e.g. optional job or conditional step); or run on schedule/nightly. Use a dedicated test DB (Testcontainers or CI Postgres) and a known-good server build (e.g. `dotnet run` or published binary).
- **Timeout:** Per-prompt timeout (e.g. 60–120 seconds) and global script timeout (e.g. 10–15 minutes) to avoid hangs.

### 5.8 Failure handling

- On **state mismatch:** Fail immediately; log the prompt index, expected vs actual state (or diff); optionally capture server logs and a DB dump (e.g. pg_dump or export of relevant tables) as artifacts.
- On **CLI timeout or crash:** Fail the test; capture stdout/stderr and server logs for debugging.

---

## 6. Manual testing

### 6.1 Manual test checklist

Use this with Cursor (or another MCP client) after deployment or before release.

| # | Area | Steps | Pass criteria |
|---|------|--------|----------------|
| 1 | Server start | Add server to Cursor config; restart or reload | Server starts; no errors in log. |
| 2 | Tools visible | Open MCP / tools panel | scope_set, scope_get, enterprise_*, project_*, requirement_*, standard_*, work_item_*, task_*, issue_*, milestone_*, release_*, domain_*, system_*, asset_*, resource_*, keyword_*, work_queue_*, association tools visible. |
| 3 | Resources visible | Open resources or equivalent | project://current/spec, /tasks, /plan listed. |
| 4 | scope_set | Call scope_set(scope_slug) with valid slug | Returns scope; no error. |
| 5 | project_get_info | After scope_set, call project_get_info | Returns project or empty; no crash. |
| 6 | project_update | Call project_update with name, description, status | Returns updated project; project_get_info shows changes. |
| 7 | work_item_create | work_item_create(title: "Manual work item", level: "Work") | Item returned; appears in work_item_list. |
| 8 | task_create | task_create(title: "Manual test task") | Task returned; appears in task_list and in project://current/tasks. |
| 9 | task_update / task_delete | Update task; then delete | List reflects changes; deleted task gone. |
| 10 | issue_create | issue_create(title: "Manual issue") | Issue returned; appears in issue_list. |
| 11 | milestone_create / list | Create milestone; list | Milestone in list. |
| 12 | release_create / list | Create release; list | Release in list. |
| 13 | Invalid context | (If client allows) Send request without or with wrong context_key | Error response; no data. |
| 14 | REST: initialize and tools | Over HTTP: POST /mcp/initialize; then POST /mcp/tools/call (or equivalent) with X-Context-Key header; GET /mcp/resources/... with header | Same results as stdio; 401/400 without valid context_key. |
| 15 | Logging | Run a few tools; check logs (console or configured sink) | Logs contain tool name, scope/correlation_id if sent; no secrets. |

### 6.2 Exploratory testing

- Switch scope (scope_set to different project); verify project_get_info and task_list reflect new scope.
- Large task list; filters (status, assignee, milestoneId); confirm performance and correctness.
- Invalid inputs: missing title on task_create, missing id on task_update; confirm structured error and no crash.

---

## 7. Test data and fixtures

### 7.1 Minimal seed (integration/E2E)

- **Enterprise:** 1 row (e.g. id, name "Test Enterprise").
- **Project:** 1 row (enterprise_id, name "Test Project", status active).
- **Requirements:** 0–2 requirements (optional) for list/filter tests.
- **Work items:** 0–2 work items (optional) for list/filter tests.
- **Tasks:** 0–2 tasks (optional) for list/filter tests.
- **Milestone:** 1 (optional) for milestone_id filter.
- **Release:** 1 (optional) for release_id.

### 7.2 Fixtures and builders

- Use in-memory builders or small fixture classes to create valid DTOs (e.g. CreateTaskRequest with default title) to keep test code readable and avoid duplication.
- No production data; all test data is synthetic and safe to delete or roll back.

---

## 8. Environments and configuration

| Environment | Purpose | Database | Logging |
|-------------|---------|----------|---------|
| **Local dev** | Developer machine | Local Postgres or Docker | Console or local Seq/file |
| **CI** | Unit + integration (and E2E) | Testcontainers or CI Postgres | Console; capture on failure |
| **Staging / pre-prod** | Manual and E2E before release | Dedicated DB; migrations applied | Configurable sink (e.g. Seq) |
| **Production** | Live use | Production DB | Configurable sink; no secrets in logs |

- **Secrets:** Never commit connection strings or API keys. Use env vars or secret store in CI and staging/prod.
- **Test env vars:** Document in README or CI workflow: e.g. `TEST_DATABASE_URL`.

---

## 9. Quality gates and CI

| Gate | When | Action |
|------|------|--------|
| **Unit tests** | Every commit / PR | Must pass; coverage ≥ threshold for covered modules. |
| **Integration tests** | Every commit / PR | Must pass; DB from Testcontainers or CI service. |
| **E2E tests** | PR to main or nightly / tag | Must pass; optional for every commit if slow. |
| **Cursor agent CLI script test** | Nightly, or when CLI available in CI | Run prompt script; assert DB state after each step; optional skip if CLI not installed. |
| **Linting / format** | Every commit | Enforce editorconfig or dotnet format. |
| **No secrets** | Every commit | Scan (e.g. gitleaks or CI check) for accidental secrets. |
| **Manual checklist** | Before release | Complete manual checklist and log results. |

---

## 10. Summary table: what to test where

| Endpoint / area | Unit | Integration | E2E | Cursor agent script | Manual |
|-----------------|------|-------------|-----|--------------------|--------|
| scope_set / scope_get | Handler + session store | With DB (scope → project) | Full flow with context_key | Prompt step + DB assert | Checklist |
| project_get_info / project_update | Handlers, validation | Repository + handler | Yes | Prompt steps + DB assert | Yes |
| enterprise_* | Handlers, validation | Repository + handler | Optional | Optional prompt step | Yes |
| requirement_* | Handlers, validation | Repository + handler | Optional | Optional prompt step | Yes |
| standard_* | Handlers, validation | Repository + handler | Optional | Optional prompt step | Yes |
| work_item_* | Handlers, validation | Repository + handler + filters | Yes | Optional prompt step | Yes |
| task_* | Handlers, validation | Repository + handler + filters | Yes | Prompt steps + DB assert | Yes |
| issue_* | Handlers, validation | Repository + handler + filters | Optional | Optional prompt step | Yes |
| milestone_* / release_* | Handlers | Repository + handler | Optional | Prompt steps + DB assert | Yes |
| domain_* / system_* | Handlers | Repository + handler | Optional | Optional prompt step | Yes |
| asset_* / resource_* / keyword_* | Handlers | Repository + handler | Optional | Optional prompt step | Yes |
| work_queue_* | Handlers | Repository + handler | Optional | Optional prompt step | Yes |
| associations & dependencies | Handlers | Repository + handler | Optional | Optional prompt step | Yes |
| Resources (spec/tasks/plan) | — | Handler + store returns correct data | Yes (resource read) | Agent may use resources | Yes |
| Context key validation | Middleware/handler | Reject invalid key | E2E without key | — | Try invalid key |
| Logging (correlation_id, no secrets) | — | Optional (assert log props) | — | — | Manual check |
| **TODO library (View, SlugService, scope, audit)** | SlugService unit; View unit (mocked); AddTodoEngine DI | View + DB; SlugService + DB; scope rejection; AuditContext; slug propagation; optional SQLite | — | — | — |
| **Blazor Web App** | UserScope, scope enforcement, SUDO, TreeService (mock), DTO validation | Auth callback; all scoped services + DB; TreeService; Search/Reports/Gantt; SUDO create enterprise | Login, ScopeGuard, tree, CRUD, search, reports, Gantt, issues, SUDO | — | Detailed plan in [24](24-blazor-webapp-testing-plan.html) |

**Blazor Web App:** Excruciatingly detailed testing for the web app (auth, scope, tree, search, reports, Gantt, issues, SUDO) is in [24 — Blazor Web App Testing Plan](24-blazor-webapp-testing-plan.html).

This plan ensures coverage of all MCP endpoints, path safety, validation, and scope; supports CI quality gates; includes a Cursor agent CLI script-driven integration test that asserts database state after each prompt; and provides a clear manual checklist for release.
