---
title: Gap Analysis — Remediation Tracker
---

# Gap Analysis — Remediation Tracker

This document lists **gaps** in the current design and documentation in **todo format** for tracking remediation. Each gap has a unique **identifier** (e.g. **GAP-001**) so it can be referenced in chat or in other docs. Mark items as completed by changing `[ ]` to `[x]` and optionally adding a "Resolved" note.

**How to use:** Reference a gap by id in conversation (e.g. "fix GAP-003") or link to this doc. When a gap is remediated, check the box and add a short "Resolved: …" line.

**Categories:** Definitions & terms | Data model & schema | Identifiers | MCP surface | Web/Mobile & auth | Implementation plan | Security & compliance | Testing & deployment | Cross-cutting

---

## Definitions and terms

- [x] **GAP-001** — **Release** has no definition in [00 — Definitions](00-definitions.html). ~~Releases appear in the schema (name, tag_version, date, notes) and in planning; the relationship of a release to milestones, requirements, or work is not defined.~~ *Resolved: Removed Release from definitions; it was a leftover from an original Scrum assumption. Schema/MCP may still have a release table or tool for version/delivery tagging; that is implementation detail, not a canonical term.*

- [x] **GAP-002** — **Doc** has no definition in [00 — Definitions](00-definitions.html). ~~Docs appear in the schema (project_id, name, path, type, description).~~ *Resolved: Removed concept of Doc from definitions. Enterprise no longer lists docs; Project no longer lists docs (standards remain). Schema/MCP may retain a docs table or tools for project file references as implementation detail; not a canonical term.*

- [x] **GAP-003** — **"Open work"** is used in the Issue definition but not defined. *Resolved: Defined in 00-definitions: open work = work items that have not been completed (state is not Complete).*

- [x] **GAP-004** — **"Sub-work"** is used (e.g. "tasks and sub-work under a work item") but not defined. *Resolved: Defined in 00-definitions: sub-work = a work item that has a parent work item. Data model (03) updated: work_items have parent_work_item_id (FK, nullable) for sub-work hierarchy.*

---

## Data model and schema

- [x] **GAP-005** — **Task ↔ requirement:** ~~Tasks … only path is via work_item_requirements.~~ *Resolved: Work items and tasks are the same entity with **Level** (`Work` | `Task`). Single table `work_items` with level; both levels link to requirements via **work_item_requirements**. No separate task table; task ↔ requirement is satisfied by the same requirement-association table for all items.*

- [ ] **GAP-006** — **Work (parent) ↔ requirement:** Work has no direct link to requirements; only work items have work_item_requirements. "All work associated with the requirements for that milestone" is only well-defined for work items. *Remediate: Decide if work should link to requirements (e.g. work_requirements) or document that association is only via work items.*

- [x] **GAP-007** — **Task state vs. milestone/work item state:** ~~Tasks use status; work items and milestones use state; mapping undefined.~~ *Resolved by GAP-005: unified entity has both **state** (level = Work, and for milestone progression) and **status** (level = Task: todo, in-progress, done, cancelled). For milestone advancement, Task-level items are treated as achieving the six-state equivalent when **status = done** (Complete for that item); status = in-progress → Implementation; status = todo → Planning; cancelled can be excluded or treated as not Complete. Single entity allows a consistent rule.*

- [x] **GAP-008** — **Time budget per resource:** ~~Schema had single resource_id and no table for time-budget-per-resource.~~ *Resolved: Added **work_item_resource_effort** (work_item_id, resource_id, **resource_effort_expected**, **resource_effort_actual**). Unique (work_item_id, resource_id). Enables expected and actual effort per resource per item; change tracking scope updated.*

- [x] **GAP-009** — **Requirement fields** unspecified: ~~requirements table had "and requirement fields" but no list.~~ *Resolved: Requirements now include **title**, **description**, **acceptance_criteria** (text or JSONB, nullable), **approved_by** (FK to resources, nullable), **approved_on** (timestamp, nullable). Specified in 03-data-model (Requirements section and PostgreSQL table).*

- [x] **GAP-010** — **Standard content** unspecified: ~~standards had "standard-specific content or references" but no format.~~ *Resolved: Standard entity now has **title**, **description** (text, nullable), and **detailed_notes** (text or JSONB, nullable) for full standard content, rules, or references. Specified in 03-data-model.*

- [x] **GAP-011** — **Issue lifecycle:** ~~State/status and severity/priority unspecified.~~ *Resolved: Issue **state** enum **Open** | **InProgress** | **Done** | **Closed**; **severity** and **priority** fields (enum or scale, nullable) added. Specified in 03-data-model (Issues section and PostgreSQL table).*

- [x] **GAP-012** — **Milestone reporting scope** not persisted: ~~No stored scope.~~ *Resolved: Milestones have **project_id** (FK to project, nullable) — scope includes that project and its sub-projects; and **milestone_scope_milestones** (milestone_id, scope_milestone_id) — total scope includes those other milestones. Together they define the milestone’s full scope for reporting and state progression. Specified in 03-data-model.*

- [x] **GAP-013** — **Work–work and work item–work item dependencies:** ~~No dependency between work entities or between work items.~~ *Resolved by combining work, work item, and task into one entity (work_items with Level Work | Task): **item_dependencies** applies to any work_items (both levels), so work–work, work item–work item, and task–task dependencies are all supported in the same table. No separate work_dependencies or work_item_dependencies needed.*

- [ ] **GAP-014** — **Approval semantics:** State enum includes "Approval." Who approves, what is approved, and what "achieved Approval" means for a work item or milestone is not defined; no approver entity or approval event. *Remediate: Define Approval (e.g. approver resource, approval event or timestamp); add to model if needed.*

- [x] **GAP-015** — **Doc vs. Asset boundary** unclear: ~~docs are project-level (path, type); assets are enterprise-level IP.~~ *Resolved by GAP-002: Doc concept removed; project-level file references are implementation detail or Assets; no separate Doc entity in definitions.*

---

## Identifiers

- [ ] **GAP-016** — **Milestone** slug/prefix not in [08 — Identifiers](08-identifiers.html) examples: owner (enterprise) and type prefix/slug pattern not explicitly given. *Remediate: Add Milestone to 08 with owner = enterprise, prefix (e.g. M or MS), and slug example.*

- [ ] **GAP-017** — **Release** slug/prefix not in 08 examples: owner (project) and type prefix/slug pattern not explicitly given. *Remediate: Add Release to 08 with owner = project, prefix (e.g. R or REL), and slug example.*

- [ ] **GAP-018** — **Issue** slug/prefix not in 08 examples: owner (project? work item?) and type prefix/slug pattern not explicitly given. *Remediate: Add Issue to 08 with owner and prefix and slug example.*

- [x] **GAP-019** — **Doc** slug/prefix not in 08 examples. *Resolved: Doc concept removed (GAP-002); no Doc entity in identifiers.*

- [ ] **GAP-020** — **Keyword** slug/identifier: 08 states "same pattern applies" but keywords may be name/label-only; confirm if keyword has display_id/slug or only id + label. *Remediate: Confirm keyword identifier rule in 08 (and 03).*

---

## MCP surface

- [ ] **GAP-021** — **MCP tools missing for many entities:** [04 — MCP Surface](04-mcp-surface.html) defines tools for project, tasks, milestones, releases, docs. No MCP tools for: enterprise (create/list/get — create restricted to SUDO), requirements, standards, work, work items, work queue, issues, domains, assets, resources, keywords, or dependency management. *Remediate: Either add tools for these in 04 (and implementation plan) or document v1 scope and add a "Future MCP tools" list.*

- [ ] **GAP-022** — **MCP resources narrow:** Read-only resources are project://current/spec, /tasks, /plan. No resources for requirements, work items, issues, or enterprise-level data. *Remediate: Document as v1 scope; optionally add resource URIs for requirements/plan detail or defer.*

- [ ] **GAP-023** — **task_create assignee:** 04 says assignee on task_create; data model uses resource_id (FK to resources). MCP surface should specify assignee as resource id or slug. *Remediate: Clarify in 04 that assignee is resource_id (GUID) or resource slug.*

---

## Web app, mobile app, and auth

- [ ] **GAP-024** — **MCP session has no resource identity:** Change tracking and logging require resource identifier; MCP sessions have context_key and scope but no authenticated "user" or resource_id. *Remediate: Define how resource_id is set for MCP-originated changes (e.g. null; or optional config mapping context_key to a service account resource).*

- [ ] **GAP-025** — **Scope source for web/mobile:** Enterprise and project scope (allowed_enterprises, allowed_projects) are "app-defined" but the source (DB table, admin UI, or IdP mapping) is not specified. *Remediate: Document where scope is stored (e.g. user_enterprise_access, user_project_access) and how it is populated.*

- [ ] **GAP-026** — **SUDO assignment:** SUDO role is "claim or app-defined role mapping" but how a user gets SUDO (config list, DB, IdP group) is not specified. *Remediate: Document SUDO assignment (e.g. env/list of GitHub user ids, or DB role table).*

---

## Implementation plan

- [ ] **GAP-027** — **Phase 1 schema scope vs. full model:** Implementation plan Phase 1 creates enterprise, project, task, milestone, release, docs (and 1.9 change tracking). Full [03 — Data Model](03-data-model.html) also includes domains, systems, assets, resources, resource_team_members, work, work_items, work_queue_items, requirements, issues, standards, keywords, entity_keywords, project_dependencies, task_dependencies, issue_requirements, work_item_requirements, system_requirement_associations. *Remediate: Add phases or tasks to implement remaining entities, or add "Implementation plan v1 scope" section that explicitly defers domains, work, requirements, issues, etc., with a follow-on phase.*

- [ ] **GAP-028** — **Change tracking and MCP resource_id:** Task 1.9 requires resource identifier on change records; MCP does not have a user/resource. *Remediate: In implementation plan or 06, state that for MCP requests resource_id may be null unless a mapping (e.g. service account per context_key) is configured.*

- [ ] **GAP-029** — **Web app and mobile app not in implementation plan:** [14 — Project Web App](14-project-web-app.html) and [15 — Mobile App](15-mobile-app.html) are specified but the implementation plan only covers MCP server and Phase 9 (GitHub OAuth2). No phases for Blazor web app or Avalonia mobile app. *Remediate: Add Phase 10 (or separate track) for web app (Blazor, tree, search, reports, Gantt, issues) and Phase 11 for mobile app (Avalonia, task queue), or document as separate delivery.*

---

## Security and compliance

- [ ] **GAP-030** — **scope_set validation:** scope_set accepts enterprise_id and project_id; docs say "validate the ids exist." Whether the agent is allowed to set scope to an arbitrary enterprise/project (vs. a fixed list per deployment) is not defined. *Remediate: Define scope_set policy: e.g. agent may only set scope to enterprises/projects from an allow-list, or any that exist; document in 04 or 06.*

- [ ] **GAP-031** — **Audit log query API:** Change tracking records are written; "query APIs for audit history can be added later." No specification for who can query audit log or what endpoints. *Remediate: Add optional "Audit / history API" to 14 or 06 (e.g. read-only, SUDO or admin only, filter by entity/date/session/resource).*

- [ ] **GAP-032** — **Log retention:** Logging and change tracking retention are "operational concerns"; no minimum retention or deletion policy. *Remediate: Document recommended retention (or "as per policy") and that cross-enterprise attempt logs be retained for follow-up.*

---

## Testing and deployment

- [ ] **GAP-033** — **Cursor agent CLI script test dependency:** [12 — Testing Plan](12-testing-plan.md) Cursor agent CLI script test requires Cursor agent CLI; may not be available in all CI environments. *Remediate: Document skip condition and optional CI job; ensure script is robust when CLI missing.*

- [ ] **GAP-034** — **E2E and enterprise scope:** E2E tests use scope_set and tools; no explicit test for "cross-enterprise access rejected and logged." *Remediate: Add E2E or integration test that attempts access to another enterprise and asserts 403 + log entry.*

- [ ] **GAP-035** — **Change tracking tests:** No explicit test that change tracking records session_id, resource_id, correlation_id and that inner exceptions produce recursive log entries. *Remediate: Add integration or unit tests for audit record shape and exception logging behavior.*

---

## Cross-cutting and docs

- [ ] **GAP-036** — **09-gaps-analysis.md outdated:** [09 — Gaps Analysis](09-gaps-analysis.md) states Domain, Asset, Resource are "not persisted"; [03 — Data Model](03-data-model.html) now has domains, assets, resources tables. *Remediate: Update 09 to remove or qualify "entities not persisted" for Domain, Asset, Resource; or supersede 09 by this tracker (16) and archive 09.*

- [ ] **GAP-037** — **Full-text search schema:** [14 — Project Web App](14-project-web-app.html) and implementation plan mention PostgreSQL FTS (tsvector/GIN); no migration or schema task for FTS columns/indexes. *Remediate: Add implementation plan task for adding tsvector columns and GIN indexes for searchable entities.*

- [ ] **GAP-038** — **Keyword and entity_keywords in Phase 1:** Data model has keywords and entity_keywords; implementation plan Phase 1 does not include them. *Remediate: Add keyword/entity_keywords to Phase 1 migration and repositories, or document as later phase.*

---

## Summary by identifier

| ID       | Category        | One-line summary |
|----------|-----------------|------------------|
| GAP-001  | Definitions     | ~~Release undefined in 00~~ Resolved: removed from 00 |
| GAP-002  | Definitions     | ~~Doc undefined in 00~~ Resolved: Doc concept removed |
| GAP-003  | Definitions     | ~~"Open work" undefined~~ Resolved: defined in 00 |
| GAP-004  | Definitions     | ~~"Sub-work" undefined~~ Resolved: sub-work = work item with parent work item |
| GAP-005  | Data model      | ~~Task ↔ requirement~~ Resolved: unified entity with Level, same requirement link |
| GAP-006  | Data model      | Work ↔ requirement link missing |
| GAP-007  | Data model      | ~~Task status vs. six-state~~ Resolved by GAP-005 (unified entity; mapping defined) |
| GAP-008  | Data model      | ~~Time budget per resource~~ Resolved: work_item_resource_effort (expected/actual) |
| GAP-009  | Data model      | ~~Requirement fields~~ Resolved: description, acceptance_criteria, approved_by, approved_on |
| GAP-010  | Data model      | ~~Standard content~~ Resolved: title, description, detailed_notes |
| GAP-011  | Data model      | ~~Issue state/severity/priority~~ Resolved: state enum, severity, priority |
| GAP-012  | Data model      | ~~Milestone scope~~ Resolved: project_id + milestone_scope_milestones |
| GAP-013  | Data model      | ~~Work/work item dependencies~~ Resolved: unified entity; item_dependencies for all |
| GAP-014  | Data model      | Approval semantics undefined |
| GAP-015  | Data model      | ~~Doc vs. Asset boundary~~ Resolved by GAP-002 |
| GAP-016  | Identifiers     | Milestone slug/prefix not in 08 |
| GAP-017  | Identifiers     | Release slug/prefix not in 08 |
| GAP-018  | Identifiers     | Issue slug/prefix not in 08 |
| GAP-019  | Identifiers     | ~~Doc slug/prefix~~ Resolved: Doc removed (GAP-002) |
| GAP-020  | Identifiers     | Keyword identifier rule confirm |
| GAP-021  | MCP surface     | MCP tools missing for many entities |
| GAP-022  | MCP resources  | MCP resources narrow |
| GAP-023  | MCP surface     | task_create assignee semantics |
| GAP-024  | Auth            | MCP session has no resource identity |
| GAP-025  | Auth            | Scope source for web/mobile unspecified |
| GAP-026  | Auth            | SUDO assignment mechanism unspecified |
| GAP-027  | Impl plan       | Phase 1 schema vs. full model |
| GAP-028  | Impl plan       | Change tracking resource_id for MCP |
| GAP-029  | Impl plan       | Web/mobile app not in implementation plan |
| GAP-030  | Security        | scope_set validation policy |
| GAP-031  | Security        | Audit log query API unspecified |
| GAP-032  | Security        | Log/audit retention unspecified |
| GAP-033  | Testing         | Cursor CLI test dependency |
| GAP-034  | Testing         | Cross-enterprise access test |
| GAP-035  | Testing         | Change tracking and exception logging tests |
| GAP-036  | Cross-cutting   | 09-gaps-analysis outdated |
| GAP-037  | Cross-cutting   | Full-text search schema task |
| GAP-038  | Cross-cutting   | Keywords in Phase 1 |

---

*Total: 38 gaps. Use the identifier (e.g. GAP-012) in chat or docs to reference a specific gap.*
