# TodoEngine schema audit

This document is the canonical checklist for table/column correctness across:
- **InitialCreate** migration (`20260209180000_InitialCreate.cs`)
- **AddProjectResources** migration (`20260209184500_AddProjectResources.cs`)
- **DatabaseBootstrap** (raw SQL when migrations are not applied)
- **Entities** (`Models/Entities.cs`) + **TodoEngineDbContext** (`Data/TodoEngineDbContext.cs`)

Column names in the DB are **snake_case** (e.g. `display_id`, `created_at`). EF uses **UseSnakeCaseNamingConvention()** for Postgres, so C# properties map as: `Id`→`id`, `DisplayId`→`display_id`, `CreatedAt`→`created_at`, etc.

---

## enterprises

| Column        | Type         | Nullable | Entity property | Migration/Bootstrap |
|---------------|--------------|----------|-----------------|---------------------|
| id            | UUID         | NO (PK)  | Id              | ✓                   |
| display_id    | TEXT         | NO       | DisplayId       | ✓                   |
| name          | TEXT         | NO       | Name            | ✓                   |
| description   | TEXT         | YES      | Description     | ✓                   |
| created_at    | TIMESTAMPTZ  | NO       | CreatedAt       | ✓                   |
| updated_at    | TIMESTAMPTZ  | NO       | UpdatedAt       | ✓                   |

Index: `ix_enterprises_display_id` UNIQUE (display_id).

---

## projects

| Column         | Type         | Nullable | Entity property | Migration/Bootstrap |
|----------------|--------------|----------|-----------------|---------------------|
| id             | UUID         | NO (PK)  | Id              | ✓                   |
| display_id     | TEXT         | NO       | DisplayId       | ✓                   |
| enterprise_id  | UUID         | NO (FK)  | EnterpriseId    | ✓                   |
| name           | TEXT         | NO       | Name            | ✓                   |
| description    | TEXT         | YES      | Description     | ✓                   |
| status         | INTEGER      | NO       | Status (enum)   | ✓                   |
| tech_stack_json| TEXT         | YES      | TechStackJson   | ✓                   |
| created_at     | TIMESTAMPTZ  | NO       | CreatedAt       | ✓                   |
| updated_at     | TIMESTAMPTZ  | NO       | UpdatedAt       | ✓                   |

Index: `ix_projects_enterprise_id_display_id` UNIQUE (enterprise_id, display_id).

---

## resources

| Column        | Type         | Nullable | Entity property | Migration/Bootstrap |
|---------------|--------------|----------|-----------------|---------------------|
| id            | UUID         | NO (PK)  | Id              | ✓                   |
| display_id    | TEXT         | NO       | DisplayId       | ✓                   |
| enterprise_id | UUID         | NO (FK)  | EnterpriseId    | ✓                   |
| name          | TEXT         | NO       | Name            | ✓                   |
| description   | TEXT         | YES      | Description     | ✓                   |
| oauth2_sub    | TEXT         | YES      | OAuth2Sub       | ✓                   |
| created_at    | TIMESTAMPTZ  | NO       | CreatedAt       | ✓                   |
| updated_at    | TIMESTAMPTZ  | NO       | UpdatedAt       | ✓                   |

Index: `ix_resources_enterprise_id_display_id` UNIQUE (enterprise_id, display_id).

---

## work_items

| Column        | Type             | Nullable | Entity property | Migration/Bootstrap |
|---------------|------------------|----------|-----------------|---------------------|
| id            | UUID             | NO (PK)  | Id              | ✓                   |
| display_id    | TEXT             | NO       | DisplayId       | ✓                   |
| project_id    | UUID             | NO (FK)  | ProjectId       | ✓                   |
| parent_id     | UUID             | YES (FK) | ParentId        | ✓                   |
| level         | INTEGER          | NO       | Level (enum)    | ✓                   |
| state         | INTEGER          | YES      | State (enum)    | ✓                   |
| status        | INTEGER          | YES      | Status (enum)   | ✓                   |
| title         | TEXT             | NO       | Title           | ✓                   |
| description   | TEXT             | YES      | Description     | ✓                   |
| resource_id   | UUID             | YES (FK) | ResourceId      | ✓                   |
| milestone_id  | UUID             | YES      | MilestoneId     | ✓                   |
| release_id    | UUID             | YES      | ReleaseId       | ✓                   |
| start_date    | TIMESTAMPTZ      | YES      | StartDate       | ✓                   |
| due_date      | TIMESTAMPTZ      | YES      | DueDate         | ✓                   |
| effort_hours  | DOUBLE PRECISION| YES      | EffortHours     | ✓                   |
| complexity    | INTEGER          | YES      | Complexity      | ✓                   |
| priority      | INTEGER          | YES      | Priority        | ✓                   |
| created_at    | TIMESTAMPTZ      | NO       | CreatedAt       | ✓                   |
| updated_at    | TIMESTAMPTZ      | NO       | UpdatedAt       | ✓                   |

Index: `ix_work_items_project_id_display_id` UNIQUE (project_id, display_id).  
Note: milestone_id, release_id have no FK in SQL (optional refs).

---

## milestones

| Column        | Type         | Nullable | Entity property | Migration/Bootstrap |
|---------------|--------------|----------|-----------------|---------------------|
| id            | UUID         | NO (PK)  | Id              | ✓                   |
| display_id    | TEXT         | NO       | DisplayId       | ✓                   |
| enterprise_id | UUID         | NO (FK)  | EnterpriseId    | ✓                   |
| project_id    | UUID         | YES (FK) | ProjectId       | ✓                   |
| title         | TEXT         | NO       | Title           | ✓                   |
| description   | TEXT         | YES      | Description     | ✓                   |
| due_date      | TIMESTAMPTZ  | YES      | DueDate         | ✓                   |
| state         | INTEGER      | NO       | State (enum)    | ✓                   |
| created_at    | TIMESTAMPTZ  | NO       | CreatedAt       | ✓                   |
| updated_at    | TIMESTAMPTZ  | NO       | UpdatedAt       | ✓                   |

Index: `ix_milestones_enterprise_id_display_id` UNIQUE (enterprise_id, display_id).

---

## releases

| Column        | Type         | Nullable | Entity property | Migration/Bootstrap |
|---------------|--------------|----------|-----------------|---------------------|
| id            | UUID         | NO (PK)  | Id              | ✓                   |
| display_id    | TEXT         | NO       | DisplayId       | ✓                   |
| project_id    | UUID         | NO (FK)  | ProjectId       | ✓                   |
| name          | TEXT         | NO       | Name            | ✓                   |
| tag_version   | TEXT         | YES      | TagVersion      | ✓                   |
| release_date  | TIMESTAMPTZ  | YES      | ReleaseDate     | ✓                   |
| notes         | TEXT         | YES      | Notes           | ✓                   |
| created_at    | TIMESTAMPTZ  | NO       | CreatedAt       | ✓                   |
| updated_at    | TIMESTAMPTZ  | NO       | UpdatedAt       | ✓                   |

Index: `ix_releases_project_id_display_id` UNIQUE (project_id, display_id).

---

## requirements

| Column        | Type         | Nullable | Entity property | Migration/Bootstrap |
|---------------|--------------|----------|-----------------|---------------------|
| id            | UUID         | NO (PK)  | Id              | ✓                   |
| display_id    | TEXT         | NO       | DisplayId       | ✓                   |
| project_id    | UUID         | NO (FK)  | ProjectId       | ✓                   |
| parent_id     | UUID         | YES (FK) | ParentId        | ✓                   |
| title         | TEXT         | NO       | Title           | ✓                   |
| description   | TEXT         | YES      | Description     | ✓                   |
| state         | INTEGER      | NO       | State (enum)    | ✓                   |
| created_at    | TIMESTAMPTZ  | NO       | CreatedAt       | ✓                   |
| updated_at    | TIMESTAMPTZ  | NO       | UpdatedAt       | ✓                   |

Index: `ix_requirements_project_id_display_id` UNIQUE (project_id, display_id).

---

## standards

| Column        | Type         | Nullable | Entity property | Migration/Bootstrap |
|---------------|--------------|----------|-----------------|---------------------|
| id            | UUID         | NO (PK)  | Id              | ✓                   |
| display_id    | TEXT         | NO       | DisplayId       | ✓                   |
| enterprise_id | UUID         | NO (FK)  | EnterpriseId    | ✓                   |
| project_id    | UUID         | YES (FK) | ProjectId       | ✓                   |
| title         | TEXT         | NO       | Title           | ✓                   |
| description   | TEXT         | YES      | Description     | ✓                   |
| created_at    | TIMESTAMPTZ  | NO       | CreatedAt       | ✓                   |
| updated_at    | TIMESTAMPTZ  | NO       | UpdatedAt       | ✓                   |

Index: `ix_standards_enterprise_id_display_id` UNIQUE (enterprise_id, display_id).

---

## issues

| Column        | Type         | Nullable | Entity property | Migration/Bootstrap |
|---------------|--------------|----------|-----------------|---------------------|
| id            | UUID         | NO (PK)  | Id              | ✓                   |
| display_id    | TEXT         | NO       | DisplayId       | ✓                   |
| project_id    | UUID         | NO (FK)  | ProjectId       | ✓                   |
| title         | TEXT         | NO       | Title           | ✓                   |
| description   | TEXT         | YES      | Description     | ✓                   |
| state         | INTEGER      | NO       | State (enum)    | ✓                   |
| resource_id   | UUID         | YES (FK) | ResourceId      | ✓                   |
| created_at    | TIMESTAMPTZ  | NO       | CreatedAt       | ✓                   |
| updated_at    | TIMESTAMPTZ  | NO       | UpdatedAt       | ✓                   |

Index: `ix_issues_project_id_display_id` UNIQUE (project_id, display_id).

---

## keywords

| Column        | Type         | Nullable | Entity property | Migration/Bootstrap |
|---------------|--------------|----------|-----------------|---------------------|
| id            | UUID         | NO (PK)  | Id              | ✓                   |
| display_id    | TEXT         | NO       | DisplayId       | ✓                   |
| enterprise_id | UUID         | NO (FK)  | EnterpriseId    | ✓                   |
| name          | TEXT         | NO       | Name            | ✓                   |
| created_at    | TIMESTAMPTZ  | NO       | CreatedAt       | ✓                   |
| updated_at    | TIMESTAMPTZ  | NO       | UpdatedAt       | ✓                   |

Index: `ix_keywords_enterprise_id_display_id` UNIQUE (enterprise_id, display_id).

---

## domains

| Column        | Type         | Nullable | Entity property | Migration/Bootstrap |
|---------------|--------------|----------|-----------------|---------------------|
| id            | UUID         | NO (PK)  | Id              | ✓                   |
| display_id    | TEXT         | NO       | DisplayId       | ✓                   |
| enterprise_id | UUID         | NO (FK)  | EnterpriseId    | ✓                   |
| name          | TEXT         | NO       | Name            | ✓                   |
| description   | TEXT         | YES      | Description     | ✓                   |
| created_at    | TIMESTAMPTZ  | NO       | CreatedAt       | ✓                   |
| updated_at    | TIMESTAMPTZ  | NO       | UpdatedAt       | ✓                   |

Index: `ix_domains_enterprise_id_display_id` UNIQUE (enterprise_id, display_id).

---

## systems

| Column        | Type         | Nullable | Entity property | Migration/Bootstrap |
|---------------|--------------|----------|-----------------|---------------------|
| id            | UUID         | NO (PK)  | Id              | ✓                   |
| display_id    | TEXT         | NO       | DisplayId       | ✓                   |
| enterprise_id | UUID         | NO (FK)  | EnterpriseId    | ✓                   |
| name          | TEXT         | NO       | Name            | ✓                   |
| description   | TEXT         | YES      | Description     | ✓                   |
| created_at    | TIMESTAMPTZ  | NO       | CreatedAt       | ✓                   |
| updated_at    | TIMESTAMPTZ  | NO       | UpdatedAt       | ✓                   |

Entity: **SystemEntity**; table: **systems**. Index: `ix_systems_enterprise_id_display_id` UNIQUE.

---

## assets

| Column        | Type         | Nullable | Entity property | Migration/Bootstrap |
|---------------|--------------|----------|-----------------|---------------------|
| id            | UUID         | NO (PK)  | Id              | ✓                   |
| display_id    | TEXT         | NO       | DisplayId       | ✓                   |
| enterprise_id | UUID         | NO (FK)  | EnterpriseId    | ✓                   |
| name          | TEXT         | NO       | Name            | ✓                   |
| description   | TEXT         | YES      | Description     | ✓                   |
| asset_type    | TEXT         | YES      | AssetType       | ✓                   |
| created_at    | TIMESTAMPTZ  | NO       | CreatedAt       | ✓                   |
| updated_at    | TIMESTAMPTZ  | NO       | UpdatedAt       | ✓                   |

Index: `ix_assets_enterprise_id_display_id` UNIQUE (enterprise_id, display_id).

---

## project_resources (AddProjectResources)

| Column      | Type | Nullable | Entity property | Migration/Bootstrap |
|-------------|------|----------|-----------------|---------------------|
| project_id  | UUID | NO (PK,FK)| ProjectId       | ✓                   |
| resource_id | UUID | NO (PK,FK)| ResourceId      | ✓                   |

PK (project_id, resource_id). FKs to projects(id) and resources(id) ON DELETE CASCADE.

---

## Audit result

**All 14 tables and every column match** across InitialCreate migration, AddProjectResources migration, DatabaseBootstrap SQL, and Entities/DbContext. SeedFunWasHad migration and bootstrap seed use the same column lists for enterprises, projects, resources, and project_resources. No fixes required.

Last verified: 2026-02-09.
