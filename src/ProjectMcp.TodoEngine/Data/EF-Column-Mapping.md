# EF column name vs database column name

Every table column from migrations/bootstrap is compared to the column name EF uses: **UseSnakeCaseNamingConvention()** plus any **HasColumnName()** overrides in `TodoEngineDbContext`. Navigation properties (e.g. `Enterprise`, `Project`) are not columns and are omitted.

**Convention:** PascalCase → snake_case (e.g. `DisplayId` → `display_id`). The convention splits on capitals and lowercases, so `OAuth2Sub` would become `o_auth2sub`; we override that one to `oauth2_sub`.

---

## enterprises (table: enterprises)

| C# property | EF column name | DB column name | Match |
|-------------|----------------|----------------|-------|
| Id | id | id | ✓ |
| DisplayId | display_id | display_id | ✓ |
| Name | name | name | ✓ |
| Description | description | description | ✓ |
| CreatedAt | created_at | created_at | ✓ |
| UpdatedAt | updated_at | updated_at | ✓ |

---

## projects (table: projects)

| C# property | EF column name | DB column name | Match |
|-------------|----------------|----------------|-------|
| Id | id | id | ✓ |
| DisplayId | display_id | display_id | ✓ |
| EnterpriseId | enterprise_id | enterprise_id | ✓ |
| Name | name | name | ✓ |
| Description | description | description | ✓ |
| Status | status | status | ✓ |
| TechStackJson | tech_stack_json | tech_stack_json | ✓ |
| CreatedAt | created_at | created_at | ✓ |
| UpdatedAt | updated_at | updated_at | ✓ |

---

## project_resources (table: project_resources)

| C# property | EF column name | DB column name | Match |
|-------------|----------------|----------------|-------|
| ProjectId | project_id | project_id | ✓ |
| ResourceId | resource_id | resource_id | ✓ |

---

## work_items (table: work_items)

| C# property | EF column name | DB column name | Match |
|-------------|----------------|----------------|-------|
| Id | id | id | ✓ |
| DisplayId | display_id | display_id | ✓ |
| ProjectId | project_id | project_id | ✓ |
| ParentId | parent_id | parent_id | ✓ |
| Level | level | level | ✓ |
| State | state | state | ✓ |
| Status | status | status | ✓ |
| Title | title | title | ✓ |
| Description | description | description | ✓ |
| ResourceId | resource_id | resource_id | ✓ |
| MilestoneId | milestone_id | milestone_id | ✓ |
| ReleaseId | release_id | release_id | ✓ |
| StartDate | start_date | start_date | ✓ |
| DueDate | due_date | due_date | ✓ |
| EffortHours | effort_hours | effort_hours | ✓ |
| Complexity | complexity | complexity | ✓ |
| Priority | priority | priority | ✓ |
| CreatedAt | created_at | created_at | ✓ |
| UpdatedAt | updated_at | updated_at | ✓ |

---

## milestones (table: milestones)

| C# property | EF column name | DB column name | Match |
|-------------|----------------|----------------|-------|
| Id | id | id | ✓ |
| DisplayId | display_id | display_id | ✓ |
| EnterpriseId | enterprise_id | enterprise_id | ✓ |
| ProjectId | project_id | project_id | ✓ |
| Title | title | title | ✓ |
| Description | description | description | ✓ |
| DueDate | due_date | due_date | ✓ |
| State | state | state | ✓ |
| CreatedAt | created_at | created_at | ✓ |
| UpdatedAt | updated_at | updated_at | ✓ |

---

## releases (table: releases)

| C# property | EF column name | DB column name | Match |
|-------------|----------------|----------------|-------|
| Id | id | id | ✓ |
| DisplayId | display_id | display_id | ✓ |
| ProjectId | project_id | project_id | ✓ |
| Name | name | name | ✓ |
| TagVersion | tag_version | tag_version | ✓ |
| ReleaseDate | release_date | release_date | ✓ |
| Notes | notes | notes | ✓ |
| CreatedAt | created_at | created_at | ✓ |
| UpdatedAt | updated_at | updated_at | ✓ |

---

## resources (table: resources)

| C# property | EF column name | DB column name | Match |
|-------------|----------------|----------------|-------|
| Id | id | id | ✓ |
| DisplayId | display_id | display_id | ✓ |
| EnterpriseId | enterprise_id | enterprise_id | ✓ |
| Name | name | name | ✓ |
| Description | description | description | ✓ |
| OAuth2Sub | **oauth2_sub** (HasColumnName) | oauth2_sub | ✓ |
| CreatedAt | created_at | created_at | ✓ |
| UpdatedAt | updated_at | updated_at | ✓ |

*Without the explicit `HasColumnName("oauth2_sub")`, the convention would produce `o_auth2sub`, which does not match the DB.*

---

## requirements (table: requirements)

| C# property | EF column name | DB column name | Match |
|-------------|----------------|----------------|-------|
| Id | id | id | ✓ |
| DisplayId | display_id | display_id | ✓ |
| ProjectId | project_id | project_id | ✓ |
| ParentId | parent_id | parent_id | ✓ |
| Title | title | title | ✓ |
| Description | description | description | ✓ |
| State | state | state | ✓ |
| CreatedAt | created_at | created_at | ✓ |
| UpdatedAt | updated_at | updated_at | ✓ |

---

## standards (table: standards)

| C# property | EF column name | DB column name | Match |
|-------------|----------------|----------------|-------|
| Id | id | id | ✓ |
| DisplayId | display_id | display_id | ✓ |
| EnterpriseId | enterprise_id | enterprise_id | ✓ |
| ProjectId | project_id | project_id | ✓ |
| Title | title | title | ✓ |
| Description | description | description | ✓ |
| CreatedAt | created_at | created_at | ✓ |
| UpdatedAt | updated_at | updated_at | ✓ |

---

## issues (table: issues)

| C# property | EF column name | DB column name | Match |
|-------------|----------------|----------------|-------|
| Id | id | id | ✓ |
| DisplayId | display_id | display_id | ✓ |
| ProjectId | project_id | project_id | ✓ |
| Title | title | title | ✓ |
| Description | description | description | ✓ |
| State | state | state | ✓ |
| ResourceId | resource_id | resource_id | ✓ |
| CreatedAt | created_at | created_at | ✓ |
| UpdatedAt | updated_at | updated_at | ✓ |

---

## keywords (table: keywords)

| C# property | EF column name | DB column name | Match |
|-------------|----------------|----------------|-------|
| Id | id | id | ✓ |
| DisplayId | display_id | display_id | ✓ |
| EnterpriseId | enterprise_id | enterprise_id | ✓ |
| Name | name | name | ✓ |
| CreatedAt | created_at | created_at | ✓ |
| UpdatedAt | updated_at | updated_at | ✓ |

---

## domains (table: domains)

| C# property | EF column name | DB column name | Match |
|-------------|----------------|----------------|-------|
| Id | id | id | ✓ |
| DisplayId | display_id | display_id | ✓ |
| EnterpriseId | enterprise_id | enterprise_id | ✓ |
| Name | name | name | ✓ |
| Description | description | description | ✓ |
| CreatedAt | created_at | created_at | ✓ |
| UpdatedAt | updated_at | updated_at | ✓ |

---

## systems (entity: SystemEntity, table: systems)

| C# property | EF column name | DB column name | Match |
|-------------|----------------|----------------|-------|
| Id | id | id | ✓ |
| DisplayId | display_id | display_id | ✓ |
| EnterpriseId | enterprise_id | enterprise_id | ✓ |
| Name | name | name | ✓ |
| Description | description | description | ✓ |
| CreatedAt | created_at | created_at | ✓ |
| UpdatedAt | updated_at | updated_at | ✓ |

---

## assets (table: assets)

| C# property | EF column name | DB column name | Match |
|-------------|----------------|----------------|-------|
| Id | id | id | ✓ |
| DisplayId | display_id | display_id | ✓ |
| EnterpriseId | enterprise_id | enterprise_id | ✓ |
| Name | name | name | ✓ |
| Description | description | description | ✓ |
| AssetType | asset_type | asset_type | ✓ |
| CreatedAt | created_at | created_at | ✓ |
| UpdatedAt | updated_at | updated_at | ✓ |

---

## Summary

| Table | Mapped columns | All match |
|-------|----------------|-----------|
| enterprises | 6 | ✓ |
| projects | 9 | ✓ |
| project_resources | 2 | ✓ |
| work_items | 18 | ✓ |
| milestones | 10 | ✓ |
| releases | 9 | ✓ |
| resources | 8 | ✓ (1 explicit HasColumnName) |
| requirements | 9 | ✓ |
| standards | 8 | ✓ |
| issues | 9 | ✓ |
| keywords | 6 | ✓ |
| domains | 7 | ✓ |
| systems | 7 | ✓ |
| assets | 8 | ✓ |

**Total: 116 column mappings; all match.** The only explicit override is `Resource.OAuth2Sub` → `oauth2_sub`.

Last verified: 2026-02-09.
