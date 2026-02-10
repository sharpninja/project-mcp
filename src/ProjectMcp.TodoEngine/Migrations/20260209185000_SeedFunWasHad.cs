using Microsoft.EntityFrameworkCore.Migrations;

namespace ProjectMCP.TodoEngine.Migrations;

[Migration("20260209185000_SeedFunWasHad")]
public partial class SeedFunWasHad : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
WITH existing_enterprise AS (
    SELECT id, display_id FROM enterprises WHERE name = '#FunWasHad' LIMIT 1
),
inserted_enterprise AS (
    INSERT INTO enterprises (id, display_id, name, description, created_at, updated_at)
    SELECT 'c4d3a4c0-6f5f-4bde-9f41-8d6c5a8f2f01', 'E9999', '#FunWasHad', 'Seed enterprise for local development.', NOW(), NOW()
    WHERE NOT EXISTS (SELECT 1 FROM existing_enterprise)
    RETURNING id, display_id
),
target_enterprise AS (
    SELECT id, display_id FROM inserted_enterprise
    UNION ALL
    SELECT id, display_id FROM existing_enterprise
)
INSERT INTO projects (id, display_id, enterprise_id, name, description, status, tech_stack_json, created_at, updated_at)
SELECT
    '6c2fb7d0-4d16-4b69-b8a2-1d8e765f6c75',
    CONCAT(target_enterprise.display_id, '-P001'),
    target_enterprise.id,
    'FunWasHad',
    'Seed project for local development.',
    0,
    NULL,
    NOW(),
    NOW()
FROM target_enterprise
WHERE NOT EXISTS (
    SELECT 1 FROM projects p
    WHERE p.enterprise_id = target_enterprise.id AND p.name = 'FunWasHad'
);
""");

        migrationBuilder.Sql("""
WITH target_enterprise AS (
    SELECT id, display_id FROM enterprises WHERE name = '#FunWasHad' LIMIT 1
)
INSERT INTO resources (id, display_id, enterprise_id, name, description, oauth2_sub, created_at, updated_at)
SELECT
    '3a00b18e-6d3d-4a64-8d12-c3bb2cb8862a',
    CONCAT(target_enterprise.display_id, '-sharpninja'),
    target_enterprise.id,
    'sharpninja',
    'User account.',
    'sharpninja',
    NOW(),
    NOW()
FROM target_enterprise
WHERE NOT EXISTS (
    SELECT 1 FROM resources r
    WHERE r.enterprise_id = target_enterprise.id AND r.name = 'sharpninja'
);
""");

        migrationBuilder.Sql("""
WITH target_enterprise AS (
    SELECT id, display_id FROM enterprises WHERE name = '#FunWasHad' LIMIT 1
)
INSERT INTO resources (id, display_id, enterprise_id, name, description, oauth2_sub, created_at, updated_at)
SELECT
    '7eafef9a-9d1e-49de-9f07-4a52f0cf2a64',
    CONCAT(target_enterprise.display_id, '-copilot'),
    target_enterprise.id,
    'Copilot',
    'AI agent.',
    'copilot',
    NOW(),
    NOW()
FROM target_enterprise
WHERE NOT EXISTS (
    SELECT 1 FROM resources r
    WHERE r.enterprise_id = target_enterprise.id AND r.name = 'Copilot'
);
""");

        migrationBuilder.Sql("""
WITH target_enterprise AS (
    SELECT id, display_id FROM enterprises WHERE name = '#FunWasHad' LIMIT 1
)
INSERT INTO resources (id, display_id, enterprise_id, name, description, oauth2_sub, created_at, updated_at)
SELECT
    'd596e9f0-3f8c-4e6e-97d4-0b2b0d0a8e8a',
    CONCAT(target_enterprise.display_id, '-cursor'),
    target_enterprise.id,
    'Cursor',
    'AI agent.',
    'cursor',
    NOW(),
    NOW()
FROM target_enterprise
WHERE NOT EXISTS (
    SELECT 1 FROM resources r
    WHERE r.enterprise_id = target_enterprise.id AND r.name = 'Cursor'
);
""");

        // Add all #FunWasHad resources to the FunWasHad project (for scope / allowed_projects)
        migrationBuilder.Sql("""
INSERT INTO project_resources (project_id, resource_id)
SELECT p.id, r.id
FROM projects p
JOIN enterprises e ON e.id = p.enterprise_id AND e.name = '#FunWasHad'
JOIN resources r ON r.enterprise_id = e.id
WHERE p.name = 'FunWasHad'
ON CONFLICT (project_id, resource_id) DO NOTHING;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DELETE FROM project_resources
WHERE project_id = '6c2fb7d0-4d16-4b69-b8a2-1d8e765f6c75';
""");

        migrationBuilder.Sql("""
DELETE FROM resources
WHERE id IN (
    '3a00b18e-6d3d-4a64-8d12-c3bb2cb8862a',
    '7eafef9a-9d1e-49de-9f07-4a52f0cf2a64',
    'd596e9f0-3f8c-4e6e-97d4-0b2b0d0a8e8a'
);
""");

        migrationBuilder.Sql("""
DELETE FROM projects
WHERE id = '6c2fb7d0-4d16-4b69-b8a2-1d8e765f6c75';
""");

        migrationBuilder.Sql("""
DELETE FROM enterprises
WHERE id = 'c4d3a4c0-6f5f-4bde-9f41-8d6c5a8f2f01';
""");
    }
}
