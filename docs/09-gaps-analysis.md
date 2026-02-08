---
title: Gaps Analysis
---

# Gaps in the Methodology (Clean-Room Analysis)

This document identifies gaps in the current design **on its own terms**: missing definitions, undefined terms, entities or relationships that are referenced but not modeled, and inconsistencies. No comparison to other methodologies is made.

**Status:** Historical snapshot; **Domains, Assets, and Resources are now persisted** in the data model. See [16 — Gap Analysis (TODO)](16-gap-analysis-todo.html) for current gaps.

**Note:** **Work items and tasks are the same entity** in the current model (task = work item with level = Task). This document predates that consolidation.

---

## Definitions lacking placement in the hierarchy

Placement in the hierarchy means: (1) an explicit **owner** (parent entity), (2) a **type prefix** for the slug, and (3) a **slug example** or rule in [08 — Identifiers](08-identifiers.html). The following definitions **lack placement** (no owner + prefix + slug convention):

| Definition   | Owner / placement | Type prefix in 08? | Slug example in 08? | Lacks placement? |
|-------------|-------------------|--------------------|----------------------|-------------------|
| Enterprise  | Top-level (none)  | E                  | E1                   | No                |
| Project     | Enterprise        | P                  | E1-P001              | No                |
| Sub-project | Project (same as Project) | P          | E1-P001-P002         | No                |
| Requirement | Project or Requirement | REQ        | E1-P001-REQ0001      | No                |
| Standard    | Enterprise or Project | STD     | E1-STD0001, E1-P001-STD0001 | No     |
| Work        | Project           | W                  | E1-P001-W001         | No                |
| Work item   | Work              | WI                 | E1-P001-W001-WI0001  | No                |
| Task        | Work item or Task (parent_task_id) | TSK (six-digit zero-padded) | E1-P001-W001-WI0001-TSK000001 | No                |
| Milestone   | Enterprise        | Not specified      | None                 | **Yes**           |
| Release     | Not in 00; schema has project_id | Not specified | None    | **Yes** (and no definition) |
| Domain      | Project (implied) | Not specified      | None                 | **Yes**           |
| Issue       | Project? Work item? Requirement? | Not specified | None    | **Yes**           |
| Keyword     | Enterprise        | Not specified      | None                 | **Yes**           |
| Asset       | Enterprise        | Not specified      | None                 | **Yes**           |
| Resource    | Enterprise or Project | Not specified | None             | **Yes**           |
| Doc         | Not in 00; schema has project_id | Not specified | None    | **Yes** (and no definition) |

**Summary — definitions that lack placement in the hierarchy:** Milestone, Release, Domain, Issue, Keyword, Asset, Resource, Doc. Of these, **Release** and **Doc** also lack a definition in [00 — Definitions](00-definitions.html). (**Task** now has placement: owned by work item or task, prefix TSK, six-digit zero-padded index.)

---

## 1. Entities defined but not persisted

**Domain** — Defined as a collection of requirements pertaining to a single silo of a project. There is no `domains` table, no `domain_id` on requirements, and no slug/identifier convention for domains in [08 — Identifiers](08-identifiers.html). Domain appears only in the entity_keywords entity-type list.

**Asset** — Defined as intellectual property belonging to the enterprise (documentation, diagrams, images, video, audio, etc.). There is no `assets` table or storage. No link from project or work to assets. No slug/identifier convention for assets.

**Resource** — Defined as people, agents, tools, and third-party systems that accomplish work. There is no `resources` table. Assignee on task and work is a string; there is no first-class Resource entity with id, slug, or ownership. Work queue “filter by resource” and “assigned to the resource” assume resources exist as a concept but they are not modeled. Resource scope (“owned or used in the scope of an enterprise or project”) cannot be stored.

---

## 2. Undefined or ambiguous terms

**Open work** — The Issue definition says the issue is linked to a work item and assigned to the resource “if the issue is associated with **open work** on one or more of those requirements.” “Open work” is not defined. It is unclear whether it means work items (or work/tasks) whose state is not Complete, or some other condition.

**Sub-work** — The design refers to “tasks and sub-work” under a work item and “all tasks and sub-work” for assignment inheritance. “Sub-work” is not defined. The model has work → work items and work → tasks; there is no work item → child work items. It is unclear whether sub-work means tasks under the work item, or an undefined hierarchy.

**Release** — Releases appear in the schema and in planning (name, tag_version, date, notes) but there is **no definition** of Release in [00 — Definitions](00-definitions.html). The relationship of a release to milestones, requirements, or work is not defined.

---

## 3. Missing relationships in the data model

**Task ↔ requirement** — Tasks are defined as implementing, testing, or planning “some aspect of one or more requirements,” but there is no **task_requirements** table or `requirement_id` on tasks. The only path from requirement to execution is via work_item_requirements (work item → requirements). Tasks do not link to requirements, so “work associated with the requirements for that milestone” cannot be fully traced through tasks unless inferred indirectly.

**Work (parent) ↔ requirement** — Work has no direct link to requirements. Only work items can have work_item_requirements. So “all work associated with the requirements for that milestone” is only well-defined for work items that have work_item_requirements; the parent work entity is not linked to requirements.

**Task state vs. milestone/work item state** — Work items and milestones use state (Planning, Implementation, Deployment, Validation, Approval, Complete). Tasks use status (todo, in-progress, done, cancelled). The rule that a milestone advances when “all work associated with the requirements … has achieved that next state” does not specify how task status maps to the six states, or whether tasks must have a state field for this check.

**Time budget per resource** — Tasks are defined as having an “expected budget of time per resource.” The schema has a single assignee (string) and no table for time-budget-per-resource (e.g. task_resource_effort with resource_id, estimated_hours). The concept is defined but not modeled.

---

## 4. Incomplete or unspecified schema

**Requirement fields** — The requirements table is described as having id, display_id, parent_requirement_id, project_id, milestone_id, “and requirement fields.” Those fields (e.g. title, description, acceptance criteria, priority) are not specified.

**Standard content** — Standards have “standard-specific content or references” but the design does not define what that content is or how it is stored (inline text, document reference, etc.).

**Issue lifecycle** — Issues have “state/status” but no enum or lifecycle is defined. Severity, priority, or resolution state for issues are not specified.

**Milestone reporting scope** — “A milestone may be scoped to a single project and its sub-projects for reporting” but there is no stored scope: no `reporting_scope_project_id` on milestone and no table to persist that choice. The scope is described but not persisted.

---

## 5. Dependency and blocking scope

**Work–work and work item–work item dependencies** — Task dependencies (task_dependencies) and project dependencies (project_dependencies) exist. There is no defined dependency between work entities or between work items. The blocking rule refers to “work or task” as prerequisite; it is unclear whether one work item can depend on another, or work on work, and how that would be stored.

---

## 6. Approval and state semantics

**Approval state** — The state enum includes “Approval” (Planning, Implementation, Deployment, Validation, Approval, Complete). Who approves, what event or artifact is approved, and what “achieved Approval” means for a work item or milestone is not defined. There is no approver entity or approval event in the model.

---

## 7. Identifiers and slugs

**Missing type prefixes and ownership rules** — [08 — Identifiers](08-identifiers.html) defines E, P, REQ, STD, W, WI and states that “Task, Domain, Asset, Resource, Sub-project, etc.” will have their own prefixes. Prefixes and ownership rules for **Issue, Domain, Asset, Resource, Release** (and Task) are not specified. So those entities lack identifier conventions.

---

## 8. Doc vs. Asset

**Overlap** — Docs are project-level (path, type, description). Assets are enterprise-level IP (documentation, diagrams, images, etc.). Whether a doc can reference an asset, or whether a doc is a kind of asset, is not defined. The boundary between doc and asset is unclear.

---

## 9. MCP surface not aligned with the model

**Tools and resources** — The MCP surface (04) defines tools for project, tasks, milestones, releases, and docs. The model also includes enterprises, requirements, standards, work, work items, work queue, issues, domains, assets, resources, keywords, project dependencies, task dependencies, and work_item_requirements. There are **no MCP tools or resources** defined for: enterprise CRUD, requirements, standards, work, work items, work queue, issues, domains, assets, resources, keywords, or dependency management. Read-only resources expose only project spec, tasks, and plan — not requirements, work items, issues, or enterprise-level data.

---

## 10. Summary table

| Gap category | Examples |
|--------------|----------|
| Entity defined, not persisted | Domain, Asset, Resource |
| Term used, not defined | Open work, sub-work, Release |
| Relationship missing | Task↔requirement, Work↔requirement, task state↔milestone state |
| Concept defined, not modeled | Time budget per resource |
| Schema underspecified | Requirement fields, standard content, issue state/severity, milestone reporting scope storage |
| Dependency scope unclear | Work–work, work item–work item dependencies |
| State semantics unclear | Approval (who, what), task status vs. six states |
| Identifiers incomplete | Slug/prefix for Issue, Domain, Asset, Resource, Release, Task |
| Boundary unclear | Doc vs. Asset |
| MCP surface narrow | No tools/resources for enterprise, requirements, standards, work, work items, issues, etc. |

---

Resolving these gaps would make the methodology self-consistent and implementable without importing assumptions from other methodologies.
