---
title: Gaps Analysis
---

# Gaps in the Methodology (Clean-Room Analysis)

This document is a **historical snapshot** of gaps that were identified in the design **on its own terms**: missing definitions, undefined terms, entities or relationships not yet modeled, and inconsistencies. It is kept for reference; **current gap tracking** uses [16 — Gap Analysis (TODO)](16-gap-analysis-todo.html) with identifiers **GAP-001+**.

**Resolution status:** Many items below have been addressed in [03 — Data Model](03-data-model.html), [08 — Identifiers](08-identifiers.html), [00 — Definitions](00-definitions.html), and [04 — MCP Surface](04-mcp-surface.html). Domain, Asset, and Resource are now persisted with tables and slug rules; Milestone, Release, Issue, Keyword have identifier prefixes; requirements have explicit fields; item dependencies and work_item_resource_effort exist; approval and issue lifecycle are specified. Remaining open items (if any) should be tracked in 16.

**Note:** **Work items and tasks are the same entity** (task = work item with level = Task). This document predates that consolidation.

---

## 1. Definitions lacking placement in the hierarchy

Placement means: (1) explicit **owner**, (2) **type prefix** for the slug, and (3) **slug example** or rule in [08 — Identifiers](08-identifiers.html).

**Status:** Resolved. [08 — Identifiers](08-identifiers.html) and [03 — Data Model](03-data-model.html) now define owner, prefix (or name-based slug), and examples for Enterprise, Project, Requirement, Standard, Work/Work item/Task, Milestone (MS), Release (REL), Issue (ISS), Keyword (KW), Domain, System, Asset, and Resource.

---

## 2. Entities defined but not persisted

**Domain, Asset, Resource** were originally called out as having no tables or slug conventions.

**Status:** Resolved. [03 — Data Model](03-data-model.html) defines **domains**, **assets**, **resources** (and **resource_team_members**, **asset_types**) with ownership, fields, and FKs. [08 — Identifiers](08-identifiers.html) defines slug rules (enterprise slug + name for Domain, System, Asset, Resource); Asset and Resource references use GUID.

---

## 3. Undefined or ambiguous terms

**Open work** — Issue definition referred to “open work”; not defined.  
**Sub-work** — “Tasks and sub-work” under a work item; sub-work was undefined.  
**Release** — No definition in 00.

**Status:** Partially addressed. Release is modeled (project-owned, prefix REL) and appears in 03/08; sub-work is clarified as child work items (same table, parent_id). “Open work” can be interpreted as work items/tasks not in a terminal state; if a formal definition is needed, add to [00 — Definitions](00-definitions.html) and track in 16.

---

## 4. Missing relationships in the data model

**Task ↔ requirement**, **Work ↔ requirement**, **task state vs. milestone state**, **time budget per resource**.

**Status:** Resolved. [03 — Data Model](03-data-model.html) has work_item_requirements; tasks (same table as work items) link via work item to requirements. Task status is mapped to the six-state model for milestone progression. **work_item_resource_effort** (work_item_id, resource_id, expected/actual) provides time budget per resource.

---

## 5. Incomplete or unspecified schema

**Requirement fields**, **standard content**, **issue lifecycle**, **milestone reporting scope**.

**Status:** Resolved or specified. Requirements have title, description, acceptance_criteria, approved_by, approved_on; issue state enum (Open, InProgress, Done, Closed) and severity/priority; approval_events for Approval→Complete. Standard content and milestone reporting scope are described in 03; any remaining detail can be tracked in 16.

---

## 6. Dependency and blocking scope

**Work–work and work item–work item dependencies** were unclear.

**Status:** Resolved. [03 — Data Model](03-data-model.html) defines **item_dependencies** (dependent_item_id, prerequisite_item_id) on work_items, supporting work–work, work item–work item, and task–task dependencies.

---

## 7. Approval and state semantics

**Approval state** — Who approves, what is approved, and what “achieved Approval” means were not defined.

**Status:** Resolved. [03 — Data Model](03-data-model.html) specifies that when a work item or milestone enters Approval, an **approval_events** entry is recorded (entity_type, entity_id, approved_by resource_id, approved_at); transition to Complete only after that.

---

## 8. Identifiers and slugs

**Missing type prefixes and ownership rules** for Issue, Domain, Asset, Resource, Release, Task.

**Status:** Resolved. [08 — Identifiers](08-identifiers.html) defines prefixes and ownership for all (MS, REL, ISS, KW, Domain/System/Asset/Resource name-based slugs, WI for work/task).

---

## 9. MCP surface not aligned with the model

**Tools and resources** — MCP surface had no tools for enterprise, requirements, standards, work items, issues, etc.

**Status:** Addressed in [04 — MCP Surface](04-mcp-surface.html). Tools and resources now cover scope, enterprise_*, project_*, requirement_*, standard_*, work_item_*, task_*, issue_*, milestone_*, release_*, domain_*, system_*, asset_*, resource_*, keyword_*, work_queue_*, association tools. Any remaining gaps belong in 16.

---

## Summary table (historical)

| Gap category | Examples | Status |
|--------------|----------|--------|
| Entity defined, not persisted | Domain, Asset, Resource | Resolved (03, 08) |
| Term used, not defined | Open work, sub-work, Release | Partially resolved; open work can be formalized in 00 if needed |
| Relationship missing | Task↔requirement, time budget | Resolved (03) |
| Schema underspecified | Requirement fields, issue state, milestone scope | Resolved or specified (03) |
| Dependency scope unclear | Work–work, work item–work item | Resolved (item_dependencies) |
| State semantics unclear | Approval | Resolved (approval_events) |
| Identifiers incomplete | Prefixes for Issue, Domain, Asset, etc. | Resolved (08) |
| MCP surface narrow | No tools for enterprise, requirements, etc. | Addressed (04) |

For **new or remaining gaps**, use [16 — Gap Analysis (TODO)](16-gap-analysis-todo.html) with **GAP-XXX** identifiers.
