---
title: Gap Analysis — Remediation Tracker
---

# Gap Analysis — Remediation Tracker

This document lists **gaps** in the design and documentation in **todo format** for tracking remediation. Each gap has a unique **identifier** (e.g. **GAP-001**) so it can be referenced in chat or in other docs. Mark items as completed by changing `[ ]` to `[x]` and optionally adding a "Resolved" note.

**How to use:** Reference a gap by id (e.g. "fix GAP-003") or link to this doc. When a gap is remediated, check the box and add a short "Resolved: …" line.

**Categories for new gaps:** Definitions & terms | Data model & schema | Identifiers | MCP surface | Web/Mobile & auth | Implementation plan | Security & compliance | Testing & deployment | Cross-cutting. Use **High / Medium / Low** severity as needed.

**Note:** **Work items and tasks are the same entity**; a **task** is a **work item** with **level = Task**.

---

## Open gaps

There are currently **no open gaps**.

## Resolved gaps

- [x] **GAP-001** — **GPS.SimpleMVC package reference:** Exact NuGet package ID/version confirmed. *Remediate: confirm package name/version and update 19-todo-library-implementation.md and solution references.* Resolved: set to `GPS.SimpleMVC` `2.0.0`.
- [x] **GAP-003** — **Migration strategy:** Aspire plan leaves the EF Core migration strategy undecided (startup migrate vs. separate step). *Remediate: choose the migration approach and update 20-aspire-implementation.md and related docs.* Resolved: use startup migrations via `context.Database.Migrate()`.
- [x] **GAP-002** — **Aspire hosting API names:** App Host plan uses placeholder method names for Postgres/database/reference/Dockerfile APIs. *Remediate: verify current Aspire.Hosting APIs and update 20-aspire-implementation.md with exact method names and package IDs.* Resolved: use `Aspire.Hosting.AppHost`/`Aspire.Hosting.PostgreSQL` 13.1.0 and `AddPostgres`, `AddDatabase`, `WithReference`, `AddDockerfile`.
- [x] **GAP-004** — **Web app testing plan missing:** No dedicated Blazor web app testing plan exists (only brief references). *Remediate: author a standalone testing plan document and connect it to the global testing plan.* Resolved: added [24 — Blazor Web App Testing Plan](24-blazor-webapp-testing-plan.html).
- [x] **GAP-005** — **UI test tooling undefined:** No specific UI/component test tooling or project layout is documented. *Remediate: choose tooling (e.g. bUnit) and define test project structure and conventions.* Resolved: tooling and layout defined in [24](24-blazor-webapp-testing-plan.html).
- [x] **GAP-006** — **OAuth flow tests missing:** GitHub OAuth callback, claims mapping, and SUDO tests are not defined. *Remediate: add tests for callback handling, claim mapping, and SUDO allowlist.* Resolved: auth test cases defined in [24](24-blazor-webapp-testing-plan.html).
- [x] **GAP-007** — **Scope enforcement tests missing:** Claims-based enterprise/project scope enforcement tests are not defined end-to-end for the web app. *Remediate: add tests that verify all services and UI views are scoped and reject cross-enterprise access.* Resolved: scope enforcement tests defined in [24](24-blazor-webapp-testing-plan.html).
- [x] **GAP-008** — **Tree/CRUD UI tests missing:** Tree navigation, CRUD forms, and validation test cases are not defined. *Remediate: define component/integration tests for tree interactions, form validation, and error states.* Resolved: UI test cases defined in [24](24-blazor-webapp-testing-plan.html).
- [x] **GAP-009** — **Search/Reports/Gantt tests missing:** Data correctness and filtering tests are not defined for search, reports, and Gantt. *Remediate: define integration tests for search results, report aggregates, and Gantt datasets.* Resolved: search/report/Gantt coverage defined in [24](24-blazor-webapp-testing-plan.html).
- [x] **GAP-010** — **E2E browser automation missing:** No browser automation strategy or CI cadence is specified. *Remediate: define E2E tooling (e.g. Playwright), test accounts, and CI execution.* Resolved: Playwright strategy defined in [24](24-blazor-webapp-testing-plan.html).
- [x] **GAP-011** — **Non-functional testing missing:** Accessibility, cross-browser, and performance test coverage is not defined. *Remediate: define a11y checks, browser matrix, and performance baselines.* Resolved: non-functional checks defined in [24](24-blazor-webapp-testing-plan.html).
- [x] **GAP-012** — **Test data/env setup missing:** No plan for seed data, fixtures, or OAuth test environment exists. *Remediate: define seed datasets, fixture helpers, and test OAuth app configuration.* Resolved: test data and env setup defined in [24](24-blazor-webapp-testing-plan.html).
- [x] **GAP-013** — **Web app security testing missing:** No CSRF/XSS or authorization-bypass test coverage is defined for the Blazor web app. *Remediate: define security test cases for CSRF tokens, input sanitization, and enforced authorization boundaries.* Resolved: security test cases defined in [24](24-blazor-webapp-testing-plan.html).

To add a gap: use the next **GAP-XXX** id, follow the format below, add a row to the summary table, and place the item under the appropriate severity (High / Medium / Low) and category.

**Format:** `- [ ] **GAP-XXX** — **Title:** Description. *Remediate: …*`

**Resolved example:** `- [x] **GAP-001** — **Title:** … *Remediate: …* Resolved: added to 03 and 08.`

---

## Summary by identifier

| ID | Category | One-line summary |
|----|----------|-------------------|
| GAP-001 | Implementation plan | GPS.SimpleMVC package ID/version confirmed (resolved) |
| GAP-003 | Implementation plan | EF Core migration strategy set to startup migrate (resolved) |
| GAP-002 | Implementation plan | Aspire.Hosting API method names confirmed (resolved) |
| GAP-004 | Testing & deployment | Blazor web app testing plan created (resolved) |
| GAP-005 | Testing & deployment | UI test tooling and layout defined (resolved) |
| GAP-006 | Web/Mobile & auth | OAuth callback and SUDO tests defined (resolved) |
| GAP-007 | Security & compliance | Scope enforcement tests defined (resolved) |
| GAP-008 | Testing & deployment | Tree navigation and CRUD UI tests defined (resolved) |
| GAP-009 | Testing & deployment | Search/reports/Gantt tests defined (resolved) |
| GAP-010 | Testing & deployment | E2E browser automation strategy defined (resolved) |
| GAP-011 | Testing & deployment | a11y/cross-browser/perf tests defined (resolved) |
| GAP-012 | Testing & deployment | Test data and OAuth test env setup defined (resolved) |
| GAP-013 | Security & compliance | Web app security testing defined (resolved) |

*Total: 0 open gaps. Use this tracker for future additions.*
