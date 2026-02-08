---
title: Identifiers
---

# Identifiers

**All entities follow the same pattern:** an **immutable key (GUID)**, a **hierarchical slug**, and a **unique integer index**. The integer index is the numeric part of the slug segment (e.g. `1` in `E1`, `001` in `P001`, `0001` in `REQ0001`), allocated per owner per entity type so that no two entities of the same type under the same owner share the same index.

## Rules

1. **Immutable key (GUID)** — Every entity has a GUID (e.g. UUID) that never changes. Used for storage, references, and APIs. Never reused or re-assigned.
2. **Hierarchical slug** — A human-readable display identifier that:
   - Begins with the slug of the entity’s **current owner** (the parent in the hierarchy).
   - Is built by appending a separator and a **type prefix** plus the **unique integer index** (e.g. `P001`, `REQ0001`).
   - For top-level entities (e.g. Enterprise), there is no owner; the slug is the type prefix plus the index (e.g. `E1`).
   - For **sub-entities** (e.g. a sub-requirement under a requirement), the owner is the parent entity; the slug is the **parent’s full slug** plus separator plus type prefix plus index.
   - **Recalculated on move** — When an entity is moved to a different owner, its slug is recalculated from the new owner’s slug. The slug always reflects the current hierarchy; only the GUID is immutable.
3. **Unique integer index** — Every entity has a **unique integer index** that forms the numeric part of its slug segment (e.g. `1`, `001`, `0001`). The index is **unique per owner per entity type**: under a given owner, no two entities of the same type share the same index. Indexes are sequentially allocated (or otherwise assigned without collision). Requirements and standards are called out in the data model as using this rule; the same pattern applies to all entity types (enterprise, project, work item, milestone, release, requirement, standard, issue, domain, asset, resource, keyword, etc.).

4. **Work items (unified with Level Work | Task)** — Work and work item are the same concept; work items and tasks are the **same entity** with a **level** field (`Work` | `Task`). Items are **owned by a project or by another item** (project_id for top-level with parent_id null; parent_id FK to same table for children). The owner for slug purposes is the parent item (if `parent_id` is set) or the **project** (if top-level). Slug uses prefix **`WI`** with a **six-digit zero-padded** unique integer index (e.g. `WI000001`, `WI000002`) for all items regardless of level. The index is unique per owner per type. See examples below.

5. **Parent identified by slug (polymorphic parents)** — For entities that may have parents of **different types** (e.g. work item/task owned by project or parent item; requirement owned by project or requirement), the parent is **identified by the slug of the parent**. The child’s slug is built from that parent slug + separator + type prefix + index. The system stores or resolves the parent by slug (or by FK to the appropriate parent table) and uses the parent’s current slug when computing the child’s slug; the parent’s type does not need to be encoded in the child’s slug.

6. **Transactional slug propagation** — When an entity’s **slug changes**, **all children** (direct and indirect, i.e. all descendants) **must have their slugs updated** within the **same transaction**. Each descendant’s slug is recalculated from its parent’s (possibly new) slug. **If the transaction fails** (e.g. a child update fails, or a constraint is violated), **the change to the entity’s slug must also fail** — the whole operation is rolled back so that slug and all descendant slugs remain consistent. No partial updates: either the entity’s new slug and every affected descendant’s new slug are committed together, or none are.

7. **Domains** — Domains **belong to an enterprise**. A domain has a **unique name within that enterprise**. The domain’s slug is **enterprise slug** + separator + **name** (e.g. `E1-security`, `E1-billing`). No type prefix or integer index: the slug segment is the name (normalised as needed for uniqueness and URL-safety). If a domain’s name changes, its slug changes; any entities that reference the domain by slug must be updated in the same transaction per rule 6.

8. **Systems** — Systems **belong to exactly one enterprise** and have a **unique name within that enterprise**. The system’s slug is built the **same as a domain**: **enterprise slug** + separator + **name** (e.g. `E1-payment-api`, `E1-auth-framework`). No type prefix or integer index. If a system’s name changes, its slug changes; any references by slug must be updated in the same transaction per rule 6.

9. **Assets** — Assets **belong to an enterprise** and have a **unique name within that enterprise**. The asset’s slug is built the **same as Domain and System**: **enterprise slug** + separator + **name** (e.g. `E1-architecture-diagram`). No type prefix or integer index. **Links to assets use the immutable identifier (GUID)**, not the slug, so that references remain stable even if the asset’s name or slug changes.

10. **Resources** — Resources **belong to an enterprise** and have a **unique name within that enterprise**. The resource’s slug is built the **same as Asset** (and Domain, System): **enterprise slug** + separator + **name** (e.g. `E1-backend-team`, `E1-jane-doe`). No type prefix or integer index. **Links to resources use the immutable identifier (GUID)**, not the slug.

Slugs are used in the UI, in references in text, and for traceability (e.g. “see E1-P001-REQ0001”). The GUID remains the single source of truth for persistence and linking.

## Examples

| Entity            | Owner        | Slug example              | Notes |
|-------------------|-------------|---------------------------|--------|
| Enterprise 1      | —           | `E1`                      | Top-level; no owner. |
| Project 1          | Enterprise 1| `E1-P001`                 | Owner slug `E1` + `P` + number. |
| Requirement 1      | Project 1   | `E1-P001-REQ0001`        | Owner slug `E1-P001` + `REQ` + number. |
| Requirement 2      | Requirement 1 (sub-requirement) | `E1-P001-REQ0001-REQ0002` | Owner slug is parent requirement’s full slug. |
| Sub-project 2      | Project 1                       | `E1-P001-P002`            | Sub-project under project; same prefix `P` with new number. |
| Requirement 2 (moved) | Sub-project 2                | `E1-P001-P002-REQ0002`    | If Requirement 2 is moved to Sub-project 2, its slug is recalculated from the new owner (Sub-project 2). |
| Standard 1 (enterprise) | Enterprise 1              | `E1-STD0001`             | Standard assigned to Enterprise; owner slug `E1` + `STD` + number. |
| Standard 1 (project)  | Project 1                   | `E1-P001-STD0001`        | Standard assigned to Project 1; owner slug `E1-P001` + `STD` + number. Sequence for standards under Project 1 is independent of enterprise-level standard numbers. |
| Item 1 (level = Work, top-level) | Project 1              | `E1-P001-WI0001`         | Work item under project; owner slug `E1-P001` + `WI` + number. |
| Item 2 (level = Task) | Item 1                      | `E1-P001-WI0001-WI000002` | Task under work item; same prefix WI, six-digit index. |
| Item 3 (level = Task, sub-task) | Item 2               | `E1-P001-WI0001-WI000002-WI000003` | Sub-task under task; owner is parent item’s full slug + WI + index. |
| Milestone 1            | Enterprise 1               | `E1-MS0001`              | Milestone owned by enterprise; prefix `MS`. |
| Release 1              | Project 1                  | `E1-P001-REL0001`        | Release owned by project; prefix `REL`. |
| Issue 1                | Project 1                  | `E1-P001-ISS0001`        | Issue owned by project; prefix `ISS`. |
| Keyword 1              | Enterprise 1               | `E1-KW0001`              | Keyword owned by enterprise; prefix `KW`. |
| Domain (security)     | Enterprise 1                | `E1-security`                             | Slug = enterprise slug + hyphen + name; name unique per enterprise. |
| Domain (billing)      | Enterprise 1                | `E1-billing`                              | Same pattern. |
| System (payment API)  | Enterprise 1                | `E1-payment-api`                          | Slug = enterprise slug + hyphen + name; same as Domain. |
| System (auth framework) | Enterprise 1              | `E1-auth-framework`                       | Same pattern. |
| Asset (architecture diagram) | Enterprise 1            | `E1-architecture-diagram`                | Slug = enterprise slug + hyphen + name; references use GUID. |
| Resource (team)              | Enterprise 1            | `E1-backend-team`                        | Same slug rule as Asset; references use GUID. |
| Resource (individual)        | Enterprise 1            | `E1-jane-doe`                            | Same pattern. |

## Conventions

- **Separator** — Use a single character (e.g. `-`) between owner slug and the new segment.
- **Type prefixes** — Short, consistent prefixes per entity type (e.g. `E` Enterprise, `P` Project, `REQ` Requirement, `STD` Standard, **`WI`** Work / Work item / Task [same concept; unified entity with level Work | Task], `MS` Milestone, `REL` Release, `ISS` Issue, `KW` Keyword). **Domain**, **System**, **Asset**, and **Resource** are exceptions: their slug is **enterprise slug + hyphen + name** (no type prefix or index); name is unique per enterprise. **Asset** and **Resource** references use the immutable GUID, not the slug.
- **Sequence numbers** — Zero-padded for sortability and fixed width (e.g. `001`, `0001`). Width can vary by entity type. **Work items** (and tasks, same entity) use a **six-digit zero-padded** index (e.g. `000001`).
- **Uniqueness** — The full slug is unique within the system. The **unique integer index** (the number in the slug segment) is allocated per owner per entity type for all entities; use a sequential allocator (or equivalent) so that no two entities of the same type under the same owner share the same index.

## Data model impact

- Every entity table has: (1) **id** (GUID, primary key, immutable), (2) **display_id** or **slug** (hierarchical, derived from owner + type prefix + unique integer index), and (3) the **unique integer index** stored or derived as the numeric part of the slug segment, allocated per owner per type. See [03 — Data Model](03-data-model.html).
- For entities with polymorphic parents, the parent is identified by **parent slug** when building the child’s slug; FKs may point to the specific parent row, but the slug is built from the parent’s current slug.
- **Slug updates** must be implemented in a single transaction: when an entity’s slug is changed, recalculate and update all descendant slugs in the same transaction; on any failure, roll back the entire change.
