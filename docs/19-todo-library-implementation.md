---
title: TODO Library — Implementation Plan
---

# TODO Library — Excruciatingly Detailed Implementation Plan

This document provides an **excruciatingly detailed** implementation plan for the **TODO engine library** (e.g. `ProjectMcp.TodoEngine`). The library encapsulates work-item/task domain logic in a reusable way, is consumed by the MCP server (and optionally the web app), uses **DI extension methods** that resolve services and data contexts from `IServiceProvider`, follows the **Go4 MVC pattern** using **GPS.SimpleMvc**, and routes all TODO API interaction through a **View** interface that extends **IView**. See [02 — Architecture](02-architecture.html), [05 — Tech and Implementation](05-tech-and-implementation.html), and [06 — Tech Requirements](06-tech-requirements.html).

**References:** [03 — Data Model](03-data-model.html), [08 — Identifiers](08-identifiers.html), [11 — Implementation Plan](11-implementation-plan.html) Phase 0 (task 0.1a) and Phase 1.

---

## 1. Purpose and scope

### 1.1 Goals

- **Reusable domain logic:** Work items (level = Work | Task), projects, milestones, releases, and related operations are implemented once in the library and invoked by the MCP server (and optionally the web app API).
- **No host coupling:** The library does **not** reference MCP or HTTP; it depends only on abstractions (e.g. `IView`, data context interfaces) and is configured via DI. The host registers the data context (EF Core DbContext or repository abstractions) and calls `AddTodoEngine(services)` (or equivalent).
- **GPS.SimpleMvc and IView:** The library uses the Go4 MVC structure with **GPS.SimpleMvc**. All TODO API interaction (queries and commands that would be exposed as MCP tools or web API) goes through a **View** interface that extends **IView**. The host or the library provides the concrete View implementation that talks to the data layer.

### 1.2 Out of scope for the library

- MCP tool registration or HTTP endpoints (those live in the host).
- Session or context_key handling (handled by the host; the library receives scope and correlation_id as parameters where needed).
- OAuth or authentication (handled by the host).

---

## 2. Solution and project structure

### 2.1 Project layout

| Project | Type | Purpose |
|---------|------|---------|
| **ProjectMcp.TodoEngine** | Class library (.NET 10) | Core TODO domain: entities (if not in a shared project), services, View interface, GPS.SimpleMvc integration. No reference to MCP or ASP.NET Core. |
| **ProjectMcp.TodoEngine.Data** (optional) | Class library | EF Core entities, DbContext, migrations. Can be merged into TodoEngine if preferred. |
| **ProjectMcp.Server** (host) | Console + Web (or Console only) | MCP server; references TodoEngine (and TodoEngine.Data if split). Registers DbContext and calls AddTodoEngine. |

**Recommendation for v1:** Single library **ProjectMcp.TodoEngine** that contains both domain logic and data access (entities, DbContext, repositories) to reduce project count. If the team prefers a clear separation, use **TodoEngine** (domain + interfaces) and **TodoEngine.Data** (EF Core only).

### 2.2 Namespaces

- **Root namespace:** `ProjectMCP` (per [06 — Tech Requirements](06-tech-requirements.html)).
- **Library namespace:** `ProjectMCP.TodoEngine` for public API (extension methods, interfaces, View). Use `ProjectMCP.TodoEngine.Data`, `ProjectMCP.TodoEngine.Services`, `ProjectMCP.TodoEngine.Views` (or similar) for internal structure.

### 2.3 Package references (TodoEngine)

- **GPS.SimpleMvc** — Exact package name and version to be confirmed; add the NuGet reference. Used for MVC (Model–View–Controller) structure.
- **Microsoft.EntityFrameworkCore** — Core package.
- **Npgsql.EntityFrameworkCore.PostgreSQL** — PostgreSQL provider.
- **Microsoft.EntityFrameworkCore.Sqlite** — SQLite (for tests or lightweight dev).
- **Microsoft.EntityFrameworkCore.SqlServer** — SQL Server (optional, for compatibility).
- No reference to **ModelContextProtocol** or **Microsoft.AspNetCore.App** in the library.

---

## 3. DI extension method (AddTodoEngine)

### 3.1 Signature and behavior

- **Method name:** `AddTodoEngine` (or `AddTodoEngineServices`).
- **Location:** Static class `TodoEngineServiceCollectionExtensions` in namespace `ProjectMCP.TodoEngine`.
- **Signature:** `IServiceCollection AddTodoEngine(this IServiceCollection services, IConfiguration? configuration = null)` or with an optional `Action<TodoEngineOptions>? configure = null`.

**Step 3.1.1:** Create `TodoEngineOptions` class with properties: `ConnectionString` (optional override), `Provider` (enum or string: Postgres, Sqlite, SqlServer). If not set, the library does **not** register a DbContext; the host must register the DbContext separately and the library only registers domain services and the View. Alternatively, if `ConnectionString` and `Provider` are set, the extension method registers the DbContext (e.g. `AddDbContext<TodoEngineDbContext>()`) so that the library is self-contained when the host wants to use it that way.

**Step 3.1.2:** Register the following in DI:

- **DbContext:** If options specify connection, register `TodoEngineDbContext` (or the chosen name) with the appropriate provider and connection string from options or from `IConfiguration` (e.g. `ConnectionStrings__DefaultConnection`, `DATABASE_URL`, `PROJECT_MCP_CONNECTION_STRING`).
- **Repositories:** Register `IProjectRepository` → `ProjectRepository`, `IWorkItemRepository` → `WorkItemRepository`, `IMilestoneRepository` → `MilestoneRepository`, `IReleaseRepository` → `ReleaseRepository` (and any other repositories the library exposes). All scoped.
- **View:** Register `ITodoView` (or the interface that extends `IView`) → concrete implementation (e.g. `TodoView` that uses the repositories). Scoped.
- **Services (optional):** If the library exposes application services (e.g. `IWorkItemService` that orchestrates repository + slug generation), register them here. Scoped.

**Step 3.1.3:** Document that the host must either (a) pass connection details via `AddTodoEngine(services, config => { config.ConnectionString = "..."; config.Provider = ... })` or (b) register the DbContext and repositories manually and only call an overload that registers the View and domain services. Prefer (a) for simplicity.

---

## 4. GPS.SimpleMvc and IView

### 4.1 Role of the library

- **Model:** Entities and DTOs (project, work item, milestone, release). The “model” in MVC is the data and its structure.
- **View:** The **View** is the interface through which “clients” (MCP tool handlers or API controllers) interact with the TODO domain. It extends **IView** (from GPS.SimpleMvc or a local interface with the same role). The View does **not** mean UI; it means the abstraction for reading and writing TODO data.
- **Controller:** Optional: if GPS.SimpleMvc uses a Controller concept, the library can expose a thin controller that receives requests and delegates to the View. For minimal surface, the host can call the View directly from MCP handlers.

**Step 4.1.1:** Obtain or define **IView** (from GPS.SimpleMvc or project). The interface should expose methods that correspond to the operations the MCP tools need, for example:

- `GetProject(scope, cancellationToken)` → project DTO or null
- `CreateOrUpdateProject(scope, dto, cancellationToken)` → project DTO
- `GetWorkItem(id, scope, cancellationToken)` → work item DTO or null
- `CreateWorkItem(scope, dto, cancellationToken)` → work item DTO
- `UpdateWorkItem(id, scope, dto, cancellationToken)` → work item DTO
- `ListWorkItems(scope, filters, cancellationToken)` → list of work item DTOs
- `DeleteWorkItem(id, scope, cancellationToken)` → success/failure
- `GetMilestones(scope, cancellationToken)`, `CreateOrUpdateMilestone(...)`, `ListMilestones(...)`
- `GetReleases(scope, cancellationToken)`, `CreateOrUpdateRelease(...)`, `ListReleases(...)`

Scope here is a value object or record with `EnterpriseId`, `ProjectId` (and optionally resource_id for audit). The library does not know about context_key; the host passes scope and correlation_id.

**Step 4.1.2:** Implement **TodoView** (or equivalent name) that implements **ITodoView** (extending IView). The implementation uses **IProjectRepository**, **IWorkItemRepository**, **IMilestoneRepository**, **IReleaseRepository** (injected). Each method validates scope (e.g. project belongs to enterprise), then calls the repository. If the library is responsible for slug generation and change tracking, the View (or a dedicated service) calls a **SlugService** and an **AuditContext** (session_id, resource_id, correlation_id) provided by the host per request.

**Step 4.1.3:** Ensure the View is the **single entry point** for TODO API interaction from the host. MCP tool handlers (and web API actions) must **not** call repositories directly; they call the View. This keeps the library boundary clear and allows the same logic to be reused and tested in isolation.

---

## 5. Entities and data model

### 5.1 Entity classes (in library or TodoEngine.Data)

Align with [03 — Data Model](03-data-model.html). At minimum for v1:

- **Enterprise** — Id (Guid), DisplayId (string), Name, Description, CreatedAt, UpdatedAt.
- **Project** — Id, DisplayId, EnterpriseId (FK), Name, Description, Status (enum: active, on-hold, archived), TechStack (JSON or owned entity), CreatedAt, UpdatedAt.
- **WorkItem** — Id, DisplayId, ProjectId, ParentId (nullable), Level (enum: Work, Task), State (enum for Work), Status (nullable; for Task: todo, in-progress, done, cancelled), TaskOrdering (nullable), SequenceInParent (nullable), ResourceId (nullable), Deadline, StartDate, EffortHours, Complexity, Priority, Title, Description, Labels (JSONB or array), MilestoneId, ReleaseId, CreatedAt, UpdatedAt.
- **Milestone** — Id, DisplayId, EnterpriseId, ProjectId (nullable), Title, DueDate, State (enum), Description.
- **Release** — Id, DisplayId, ProjectId, Name, TagVersion, Date, Notes.
- **Resource (minimal for Phase 1)** — Id, DisplayId, EnterpriseId, Name (for work_items.resource_id FK and agent resolution).

Use **nullable reference types**. Use enums or constants for Status, State, Level so that validation is consistent.

**Step 5.1.1:** Create one C# class per entity; add EF Core annotations or fluent configuration (e.g. `ToTable("work_items")`, `Property(w => w.Labels).HasConversion(...)` for JSONB). Ensure primary keys are Guid; foreign keys and indexes match the data model doc.

### 5.2 DbContext

- **Class name:** e.g. `TodoEngineDbContext` or `ProjectMcpDbContext`.
- **DbSets:** `Enterprises`, `Projects`, `WorkItems`, `Milestones`, `Releases`, `Resources`. Configure relationships (e.g. Project.EnterpriseId → Enterprise.Id; WorkItem.ProjectId → Project.Id; WorkItem.ParentId → WorkItem.Id).
- **Provider-agnostic:** Use provider-agnostic APIs where possible (e.g. `HasConversion` for JSON). For PostgreSQL, use Npgsql-specific extensions for JSONB and UUID if needed. For SQLite, ensure types are compatible (e.g. Guid as text).
- **Step 5.2.1:** In `OnModelCreating`, configure every entity table and relationship; add unique index on (EnterpriseId, DisplayId) or per-entity rules per [08 — Identifiers](08-identifiers.html). Add indexes for common filters (e.g. WorkItems: ProjectId, Level, Status, MilestoneId, ResourceId).

---

## 6. Repositories

### 6.1 Interfaces

Define in the library (e.g. `ProjectMCP.TodoEngine.Abstractions` or same as implementation namespace):

- **IProjectRepository:** GetById(Guid id), GetBySlug(string slug, Guid enterpriseId), Create(Project entity), Update(Project entity), Exists(Guid id).
- **IWorkItemRepository:** GetById(Guid id), GetBySlug(string slug, Guid projectId), List(WorkItemFilter filter), Create(WorkItem entity), Update(WorkItem entity), Delete(Guid id). WorkItemFilter: ProjectId, Level?, Status?, MilestoneId?, ResourceId?, ParentId?.
- **IMilestoneRepository:** GetById(Guid id), ListByEnterprise(Guid enterpriseId), Create(Milestone entity), Update(Milestone entity).
- **IReleaseRepository:** GetById(Guid id), ListByProject(Guid projectId), Create(Release entity), Update(Release entity).
- **IResourceRepository (minimal):** GetById(Guid id), GetBySlug(string slug, Guid enterpriseId), ResolveAgentNameToResource(string agentName, Guid enterpriseId) → Resource or null.

All repository methods that modify data should accept an optional **AuditContext** (SessionId, ResourceId, CorrelationId) and pass it to the change-tracking layer.

### 6.2 Implementations

- **Step 6.2.1:** Implement each repository as a class that takes `TodoEngineDbContext` (or the DbContext type) in the constructor. Use EF Core to query and save. Do **not** generate slugs or display_id in the repository; that is the responsibility of a **SlugService** or the View layer that allocates the next index and builds the slug per [08 — Identifiers](08-identifiers.html).
- **Step 6.2.2:** For **Create**, the caller (View or service) must set Id (new Guid), DisplayId (from SlugService), and other fields. Repository only performs Insert. For **Update**, load entity, apply changes, save. Enforce that updates are within scope (e.g. project’s enterprise matches session enterprise); the View or a scope-validation service should do this before calling the repository.

---

## 7. Slug and identifier generation

### 7.1 SlugService (or equivalent)

- **Responsibility:** Given an entity type and owner (e.g. project for work items, enterprise for milestones), allocate the **next unique integer index** and build the **display_id (slug)** per [08 — Identifiers](08-identifiers.html).
- **Interface:** e.g. `ISlugService`: `Task<string> AllocateSlugAsync(string entityType, string ownerSlug, CancellationToken ct)`. Owner slug examples: `E1` for enterprise-level, `E1-P001` for project-level. Entity type: `WorkItem`, `Milestone`, `Release`, `Requirement`, etc.
- **Implementation:** Query the database for the max index for that owner and entity type (e.g. max numeric part of display_id for work_items where project_id = X), then increment and format (e.g. WI000001). Use a transaction or unique constraint to avoid collisions if concurrent creates occur. For slug format rules (prefix, zero-padding), see [08 — Identifiers](08-identifiers.html).

**Step 7.1.1:** Implement SlugService; inject it into the View or into a domain service that the View uses. When creating a new WorkItem, the View (or WorkItemService) calls SlugService to get the new display_id, then passes the entity to the repository.

### 7.2 Transactional slug propagation

Per [08 — Identifiers](08-identifiers.html) and [03 — Data Model](03-data-model.html), when an entity’s **slug (display_id) changes**, all **descendant** entities’ slugs must be updated in the **same transaction**. The library must provide a way to (a) update an entity’s display_id and (b) recalculate and update all descendants’ display_ids in one transaction. Implement this in a dedicated service (e.g. `SlugPropagationService`) or inside the repository/View for the entity type that supports moves (e.g. requirement moved to another project). Document which entities support slug change and how the library handles propagation.

---

## 8. Change tracking integration

### 8.1 Audit context

- **AuditContext:** Value type or record with SessionId (string, e.g. context_key), ResourceId (Guid?), CorrelationId (string?). Passed from the host into the library for every write operation (create, update, delete).
- **Step 8.1.1:** The View (or repository) accepts AuditContext and forwards it to the persistence layer. The persistence layer (EF Core save changes interceptor, or audit table writer) records each change with session_id, resource_id, correlation_id, entity_type, entity_id, old/new values (or row state), and changed_at. Implementation options: (1) EF Core interceptors that write to an audit table, (2) database triggers with session variables set by the app, (3) application-level audit log writer. The library should define an **IAuditWriter** (or use a callback) so the host can plug in the actual audit implementation (e.g. write to `entity_change_history` table).

### 8.2 Scope enforcement

- **Step 8.2.1:** Every View method that takes a scope must **validate** that the requested operation is within that scope. For example: GetProject(scope) should load the project only if project.EnterpriseId == scope.EnterpriseId. CreateWorkItem(scope, dto) should ensure dto.ProjectId (or scope.ProjectId) belongs to scope.EnterpriseId. The library must **not** trust the caller; it must re-validate using the data in the database (e.g. load project and check enterprise_id). If validation fails, throw a domain exception (e.g. `ScopeViolationException`) that the host can map to 403 and log.

---

## 9. DTOs and API surface

### 9.1 Request/response DTOs

Define DTOs for the View so that the library does not expose EF entities to the host. Examples:

- **ProjectDto:** Id, DisplayId, Name, Description, Status, TechStack (dict or JSON), EnterpriseId, CreatedAt, UpdatedAt.
- **WorkItemDto:** Id, DisplayId, ProjectId, ParentId, Level, State, Status, Title, Description, ResourceId, MilestoneId, ReleaseId, Labels, Deadline, StartDate, EffortHours, Complexity, Priority, CreatedAt, UpdatedAt.
- **MilestoneDto**, **ReleaseDto** — similar.
- **CreateWorkItemRequest**, **UpdateWorkItemRequest** — subset of fields that the client can set.

**Step 9.1.1:** Place DTOs in a namespace like `ProjectMCP.TodoEngine.Models` or `ProjectMCP.TodoEngine.Dto`. The View returns DTOs and accepts request DTOs; internally it maps to/from entities.

---

## 10. Testing

- **Step 10.1:** Add a test project (e.g. `ProjectMcp.TodoEngine.Tests`) that references the library. Use an in-memory SQLite provider (or Testcontainers Postgres) to run migrations and exercise the View and repositories. Test: create project, create work item (task), list by project, update, delete; create milestone and release; slug allocation; scope validation (attempt to access another enterprise returns failure).

---

## 11. Task checklist (summary)

| ID | Task | Dependency |
|----|------|------------|
| T.1 | Create class library ProjectMcp.TodoEngine; add GPS.SimpleMvc, EF Core, Npgsql, Sqlite, SqlServer packages | — |
| T.2 | Define entity classes (Enterprise, Project, WorkItem, Milestone, Release, Resource) and DbContext | T.1 |
| T.3 | Create initial EF Core migration for PostgreSQL (tables per [03 — Data Model](03-data-model.html) for v1 scope) | T.2 |
| T.4 | Define repository interfaces and implement repositories | T.2 |
| T.5 | Define IView (extending IView) and ITodoView with methods for project, work item, milestone, release | T.1 |
| T.6 | Implement TodoView using repositories; add scope validation in each method | T.4, T.5 |
| T.7 | Implement SlugService (allocate index, build slug per [08 — Identifiers](08-identifiers.html)) | T.2 |
| T.8 | Integrate SlugService into View for create operations; implement transactional slug propagation for slug changes | T.6, T.7 |
| T.9 | Define AuditContext and IAuditWriter; pass AuditContext through View to persistence; implement or document audit recording | T.6 |
| T.10 | AddTodoEngine extension: register DbContext (if options provided), repositories, View, SlugService | T.6, T.7 |
| T.11 | Define DTOs and use them in View public API | T.6 |
| T.12 | Add unit/integration tests for View and slug generation | T.10 |

---

## 12. Dependencies on other phases

- **Phase 1 (Data layer):** This plan implements the library that Phase 1 uses; alternatively, Phase 1 can implement the data layer inside the host and then refactor into the library. The plan assumes the library is created in Phase 0 (task 0.1a) and refined in Phase 1.
- **Phase 2 (Session/scope):** The host provides scope (enterprise_id, project_id) and AuditContext (session_id, resource_id, correlation_id) when calling the View.
- **MCP tools (Phases 4–6):** Tool handlers in the host resolve scope from the session, then call the View (e.g. `await todoView.GetProject(scope, ct)`).

This document is the single reference for implementing the TODO library in excruciating detail.
