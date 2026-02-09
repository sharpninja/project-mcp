---
title: TODO Engine Testing Plan
---

# TODO Engine Testing Plan

This document defines a **testing plan** for the **TODO engine library** (`ProjectMcp.TodoEngine`). It focuses on the library's domain logic, View interface, repositories, and EF Core integration, and complements the broader [12 — Testing Plan](12-testing-plan.html).

**References:** [19 — TODO Library Implementation](19-todo-library-implementation.html), [03 — Data Model](03-data-model.html), [08 — Identifiers](08-identifiers.html).

**Note:** **Work items and tasks are the same entity**; a **task** is a **work item with level = Task**.

---

## 1. Scope and goals

**In scope:**
- `ProjectMcp.TodoEngine` (and optional `ProjectMcp.TodoEngine.Data`) behavior and data access.
- View interface (`IView`/`ITodoView`) as the **single entry point** to domain operations.
- EF Core mappings, migrations, and provider compatibility.
- Slug and identifier generation and propagation.
- Scope validation and audit context propagation.

**Out of scope:**
- MCP server handlers, REST endpoints, and transport-layer behavior (covered in [12 — Testing Plan](12-testing-plan.html)).
- Web app and mobile app UI testing.

---

## 2. Test pyramid and projects

| Level | Purpose | Scope | Tools / approach |
|------|---------|--------|-------------------|
| **Unit** | Fast, isolated logic | Slug service, validators, DTO mapping, scope checks, DI registration | xUnit + mocks (NSubstitute/FakeItEasy) |
| **Integration (SQLite)** | Fast DB integration | DbContext mappings, repositories, View behavior | xUnit + SQLite in-memory |
| **Integration (PostgreSQL)** | Parity with production | JSONB, constraints, indexes, migrations, audit | xUnit + Testcontainers Postgres |

**Suggested test projects:**
```
tests/
  ProjectMcp.TodoEngine.Tests.Unit/
  ProjectMcp.TodoEngine.Tests.Integration/
```

---

## 3. Unit tests

### 3.1 DI and options
- `AddTodoEngine` registers repositories, View, and services with expected lifetimes.
- When options include provider/connection string, DbContext registration occurs.
- When options are absent, library does not register DbContext (host responsibility).

### 3.2 Slug and identifier logic
- `SlugService` allocates unique display_ids per entity type and owner.
- Prefixes and zero padding match [08 — Identifiers](08-identifiers.html).
- Slug collisions result in retries or failures per design.

### 3.3 View and scope validation
- View rejects cross-enterprise and cross-project access.
- Invalid or missing scope results in domain exception.
- View methods do not bypass scope checks.

### 3.4 DTO mapping and validation
- DTO mapping handles nullable fields and defaults consistently.
- Invalid enum values or missing required fields are rejected.

### 3.5 Audit context propagation
- Write methods pass `AuditContext` (session_id, resource_id, correlation_id) into repositories or audit writer.

---

## 4. Integration tests (SQLite)

### 4.1 DbContext and repositories
- Apply migrations and validate basic CRUD for:
  - Enterprise, Project, WorkItem, Milestone, Release, Resource.
- Verify required indexes and uniqueness for display_ids.

### 4.2 View integration
- Create and update project and work items via View.
- List work items by project and filters (level, status, milestone).
- Scope enforcement uses DB state (enterprise_id, project_id).

---

## 5. Integration tests (PostgreSQL)

### 5.1 Schema parity
- Migrations apply cleanly to empty Postgres.
- JSONB fields persist and round-trip (tech_stack, labels).
- FK and unique constraints are enforced as specified in [03 — Data Model](03-data-model.html).

### 5.2 Audit and change tracking
- Insert/update/delete generate audit records with session_id, resource_id, correlation_id.
- All entity tables are included in change tracking.

---

## 6. Migration and schema verification

- Run `context.Database.Migrate()` on a fresh database.
- Validate table names, column types, and indexes match [03 — Data Model](03-data-model.html).
- Validate identifier uniqueness rules per [08 — Identifiers](08-identifiers.html).

---

## 7. Concurrency and transactions

- Slug allocation under concurrent inserts does not produce duplicates.
- Slug propagation updates descendants in a single transaction.
- Failed operations do not leave partial updates (transaction rollback).

---

## 8. Test data and helpers

- Use fixture helpers to create Enterprise + Project + Resource baseline.
- Prefer deterministic IDs and slugs for predictable assertions.
- Keep test data minimal to isolate failures.

---

## 9. Quality gates

- Unit coverage target: **>= 80%** for library logic (View, services, slug logic).
- Integration tests must pass for SQLite and Postgres in CI (Postgres can be nightly if slow).
- Fail builds on missing migrations or schema drift.
