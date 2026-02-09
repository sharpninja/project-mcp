---
title: Definitions
---

# Definitions

Canonical terms used in the design. **Methodology neutrality:** These definitions do not assume or impose any specific methodology (e.g. agile, Waterfall). The model is methodology-agnostic; process is applied by the organization.

## Enterprise

**Enterprise** — The top-level hierarchy representing ownership of projects and resources.

- All projects and their resources (tasks, milestones) are owned by an enterprise.
- The enterprise is the scope for ownership, billing, or tenant isolation when multiple organizations use the system.
- In the data model, projects reference an enterprise (e.g. `enterprise_id`); the enterprise is the root of the hierarchy.
- **Enterprise records can only be added by a user with the SUDO role.** The SUDO role is an authorization role (e.g. assigned via token claims or an app-defined role mapping); see [14 — Project Web App](14-project-web-app.html).

## Project

**Project** — A collection of requirements, standards, work, and sub-projects. Projects belong to an enterprise and contain metadata, tech stack, standards, tasks and planning (work), and optionally child projects (sub-projects). See [03 — Data Model](03-data-model.html).

## Requirement

**Requirement** — Something that a project must accomplish. Requirements are the goals or conditions the project is committed to achieving; work (tasks, milestones, etc.) is planned and executed to satisfy them. Requirements have a **parent–child relation**: one parent requirement can have zero or more child requirements (sub-requirements). See [03 — Data Model](03-data-model.html).

## Standard

**Standard** — How requirements must be accomplished. Standards define the rules, criteria, or methods that work must follow when fulfilling requirements (e.g. coding standards, security or compliance rules, documentation formats). Standards are assigned to either **Enterprises** or **Projects** (enterprise-level standards apply across the enterprise; project-level standards apply to that project and optionally its sub-projects).

## Work (work item)

**Work** and **work item** are the same concept. A single entity represents both: items under a project (top-level) or under another work item, with a **Level** field **`Work`** or **`Task`**. A single table (e.g. `work_items`) stores all of them; each row has **level** = `Work` or `Task` and **project_id** (for top-level items) or **parent_id** (for children). **Level = Work** items can have planning attributes (**deadline**, **start date**, **effort in hours**, **complexity** 1–5, **priority** 1–5), the same **states** as milestones (Planning, Implementation, Deployment, Validation, Approval, Complete), a **work queue**, and **task_ordering** for children. **Level = Task** items are leaf execution units with **status** (todo, in-progress, done, cancelled) and can link directly to requirements. Both levels can link to requirements. When the parent is another item with Level = Work, the child is **sub-work** (see [Sub-work](#sub-work)). **Completion rolls up:** when all child items are complete (state = Complete for Work; status = done for Task), the parent work item is marked **Complete**, and this check applies recursively to ancestors. **Approval semantics:** when a work item or milestone enters **Approval**, an approval event (approved_by, approved_on) is recorded; the item transitions to **Complete** only after approval is recorded. Assignment, blocking, and slug rules apply to all items. See [03 — Data Model](03-data-model.html) and [08 — Identifiers](08-identifiers.html).

## Sub-work

**Sub-work** — An item with **Level = `Work`** whose parent is another work item (i.e. **parent_id** is set to an item with level = Work, not a top-level item under the project). Sub-work inherits assignment and blocking rules from its parent work item. See [Work (work item)](#work-work-item) and [03 — Data Model](03-data-model.html).

## Task

**Task** — An item with **Level = `Task`**. An assignment to be completed by one or more resources to implement, test, or plan some aspect of one or more requirements, with an expected budget of time per resource. Tasks are **owned by a project** (top-level) **or by another item** (work item or task) for hierarchy and display id. **Tasks link directly to requirements** (same requirement-association table as work items). Tasks can be **dependent on other tasks** (e.g. task B cannot start until task A is complete). Within a work item (level = Work), tasks may be **ordered** (defined sequence) **or allowed in parallel**. See [03 — Data Model](03-data-model.html) and [08 — Identifiers](08-identifiers.html).

## Milestone

**Milestone** — A collection of requirements within an enterprise that spans zero or more projects and that are to be completed. Milestones group requirements (and the work that fulfills them) for delivery or review. **The relationship between a milestone and a project is not a hard link, but inferred through requirements**: requirements link to a milestone (requirement.milestone_id) and belong to a project (requirement.project_id), so the set of projects associated with a milestone is derived from the projects of its requirements. Milestones may be **scoped to a single project and its sub-projects for reporting**; that scope filters the requirements considered associated with the milestone. Milestones have **states**: Planning, Implementation, Deployment, Validation, Approval, Complete. **For a milestone to move to the next state, all work associated with the requirements for that milestone must have achieved that next state.** See [03 — Data Model](03-data-model.html).

## Sub-project

**Sub-project** — A collection of related requirements and work following the standards of the parent project. A sub-project is a child project that inherits or adheres to the parent’s standards while having its own requirements and work (and may have further sub-projects). **Project-to-project dependencies** are tracked from the dependent project (sub-project) to zero or more parent projects (one-to-many: one dependent project, many parent projects it depends on). See [03 — Data Model](03-data-model.html).

## Domain

**Domain** — A collection of requirements pertaining to a single silo of a project. A domain groups requirements (and the work that implements them) by a coherent area of concern (e.g. security, billing, UX), so a project can be organized into multiple domains. **A requirement can belong to at most one domain or to no domain.** **Domains belong to an enterprise** and have a **unique name within that enterprise**. The domain’s slug is the **enterprise slug**, hyphen, then the **name** (e.g. `E1-security`, `E1-billing`). See [03 — Data Model](03-data-model.html) and [08 — Identifiers](08-identifiers.html).

## System

**System** — A collection of one or more related requirements. Systems **belong to exactly one enterprise** and have a **unique name within that enterprise**. The system's slug is built the same as a domain: **enterprise slug**, hyphen, then the **name** (e.g. `E1-payment-api`). A requirement may be associated with a system **either as included in the system** (part of the system) **or as a dependency of the system** (the system depends on that requirement). Systems can be categorized as **Application**, **Framework**, **API**, or **Compound** (unions of the other categories). See [03 — Data Model](03-data-model.html) and [08 — Identifiers](08-identifiers.html).

## Open work

**Open work** — Work items that have not been completed. A work item is **open** when its state is not **Complete**; once its state is Complete, it is no longer open work. Used for issue association: an issue linked to a requirement is linked to a work item (and its assignee) when there is open work on that requirement. See [Work (work item)](#work-work-item) and [03 — Data Model](03-data-model.html).

## Issue

**Issue** — A defect in planning or implementation associated with one or more requirements. **If the issue is associated with open work on one or more of those requirements**, the issue is **linked to the work item** and **assigned to the resource** assigned to that work. **Otherwise**, the issue is **freely associated with the requirement** (no work item link) until a work item is created and the issue is assigned to it. **Assigning an issue to a work item may expand the set of requirements** associated with that work item (the work item gains the issue’s requirement associations). See [03 — Data Model](03-data-model.html).

## Keyword

**Keyword** — A tag or label used to categorize and find entities. Keywords are stored in a separate table and **scoped to a single enterprise**. All entities can have zero or more keywords; the relationship to keywords is stored with the enterprise the entity belongs to so that entities only reference keywords from their own enterprise. See [03 — Data Model](03-data-model.html).

## Asset

**Asset** — A piece of intellectual property belonging to the enterprise, such as documentation, diagrams, images, video, audio, or other creative artifacts. Assets are **linked directly to an enterprise** and follow the **same naming and slug rules as Domain and System**: unique name within the enterprise, slug = **enterprise slug** + hyphen + **name**. **Links to assets use the immutable identifier** (GUID), not the slug. Each asset has an **asset type** (from a lookup table), a **URN** to a file or online resource, and may **link to a thumbnail** (another asset or URN). Each asset type has a **default icon** specified by a URN; **if the asset has a thumbnail defined, the URN to the thumbnail overrides the default icon** for display. See [03 — Data Model](03-data-model.html) and [08 — Identifiers](08-identifiers.html).

## Resources

**Resources** — People, agents, tools, and third-party systems that are leveraged to accomplish work within a project. Resources are **directly tied to one enterprise** and follow the **same id and slug rules as Assets**: unique name within the enterprise, slug = **enterprise slug** + hyphen + **name**. **Links to resources use the immutable identifier** (GUID), not the slug. **A resource can have an OAuth2 id** (e.g. GitHub user id when GitHub is the OAuth2 provider; see [14 — Project Web App](14-project-web-app.html)) so the authenticated user can be resolved to that resource for “assigned to me” and task queues. **A resource can be a team of other resources**, so work and tasks can be assigned to teams as well as to individuals. **Team semantics:** membership is direct only (no nested teams); if an item is assigned to both a team and an individual, the individual assignment takes precedence. See [03 — Data Model](03-data-model.html) and [08 — Identifiers](08-identifiers.html).

*In MCP terms,* “resources” also refers to read-only URIs that expose project state (e.g. `project://current/spec`). Context distinguishes the domain meaning from the MCP API meaning.
