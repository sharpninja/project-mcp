using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace ProjectMCP.TodoEngine.Data;

/// <summary>Runs initial schema and seed when EF migration discovery fails (e.g. context in referenced assembly).</summary>
public static class DatabaseBootstrap
{
    /// <summary>If the database has no applied migrations and no resources table, run InitialCreate + AddProjectResources + Seed SQL and record migrations.</summary>
    public static void EnsureSchema(this DatabaseFacade database)
    {
        if (!database.IsRelational())
            return;

        var creator = database.GetService<IRelationalDatabaseCreator>();
        if (!creator.Exists())
            return;

        // Use the context's connection; do not dispose it (context owns it).
        var connection = database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            connection.Open();

        // Check if resources table exists (quick way to see if InitialCreate was applied)
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = 'resources' LIMIT 1;
                """;
            var exists = cmd.ExecuteScalar();
            if (exists is not null)
                return; // Schema already present
        }

        // Run bootstrap: same SQL as migrations, then record in history
        RunInitialCreate(connection);
        RunAddProjectResources(connection);
        RunSeedFunWasHad(connection);
        EnsureMigrationsHistory(connection);
    }

    private static void RunInitialCreate(System.Data.Common.DbConnection connection)
    {
        Execute(connection, """
            CREATE TABLE IF NOT EXISTS enterprises (
                id UUID NOT NULL PRIMARY KEY,
                display_id TEXT NOT NULL,
                name TEXT NOT NULL,
                description TEXT,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_enterprises_display_id ON enterprises (display_id);
            """);
        Execute(connection, """
            CREATE TABLE IF NOT EXISTS projects (
                id UUID NOT NULL PRIMARY KEY,
                display_id TEXT NOT NULL,
                enterprise_id UUID NOT NULL REFERENCES enterprises(id),
                name TEXT NOT NULL,
                description TEXT,
                status INTEGER NOT NULL,
                tech_stack_json TEXT,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_projects_enterprise_id_display_id ON projects (enterprise_id, display_id);
            """);
        Execute(connection, """
            CREATE TABLE IF NOT EXISTS resources (
                id UUID NOT NULL PRIMARY KEY,
                display_id TEXT NOT NULL,
                enterprise_id UUID NOT NULL REFERENCES enterprises(id),
                name TEXT NOT NULL,
                description TEXT,
                oauth2_sub TEXT,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_resources_enterprise_id_display_id ON resources (enterprise_id, display_id);
            """);
        Execute(connection, """
            CREATE TABLE IF NOT EXISTS work_items (
                id UUID NOT NULL PRIMARY KEY,
                display_id TEXT NOT NULL,
                project_id UUID NOT NULL REFERENCES projects(id),
                parent_id UUID REFERENCES work_items(id),
                level INTEGER NOT NULL,
                state INTEGER,
                status INTEGER,
                title TEXT NOT NULL,
                description TEXT,
                resource_id UUID REFERENCES resources(id),
                milestone_id UUID,
                release_id UUID,
                start_date TIMESTAMPTZ,
                due_date TIMESTAMPTZ,
                effort_hours DOUBLE PRECISION,
                complexity INTEGER,
                priority INTEGER,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_work_items_project_id_display_id ON work_items (project_id, display_id);
            """);
        Execute(connection, """
            CREATE TABLE IF NOT EXISTS milestones (
                id UUID NOT NULL PRIMARY KEY,
                display_id TEXT NOT NULL,
                enterprise_id UUID NOT NULL REFERENCES enterprises(id),
                project_id UUID REFERENCES projects(id),
                title TEXT NOT NULL,
                description TEXT,
                due_date TIMESTAMPTZ,
                state INTEGER NOT NULL,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_milestones_enterprise_id_display_id ON milestones (enterprise_id, display_id);
            """);
        Execute(connection, """
            CREATE TABLE IF NOT EXISTS releases (
                id UUID NOT NULL PRIMARY KEY,
                display_id TEXT NOT NULL,
                project_id UUID NOT NULL REFERENCES projects(id),
                name TEXT NOT NULL,
                tag_version TEXT,
                release_date TIMESTAMPTZ,
                notes TEXT,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_releases_project_id_display_id ON releases (project_id, display_id);
            """);
        Execute(connection, """
            CREATE TABLE IF NOT EXISTS requirements (
                id UUID NOT NULL PRIMARY KEY,
                display_id TEXT NOT NULL,
                project_id UUID NOT NULL REFERENCES projects(id),
                parent_id UUID REFERENCES requirements(id),
                title TEXT NOT NULL,
                description TEXT,
                state INTEGER NOT NULL,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_requirements_project_id_display_id ON requirements (project_id, display_id);
            """);
        Execute(connection, """
            CREATE TABLE IF NOT EXISTS keywords (
                id UUID NOT NULL PRIMARY KEY,
                display_id TEXT NOT NULL,
                enterprise_id UUID NOT NULL REFERENCES enterprises(id),
                name TEXT NOT NULL,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_keywords_enterprise_id_display_id ON keywords (enterprise_id, display_id);
            """);
        Execute(connection, """
            ALTER TABLE requirements ADD COLUMN IF NOT EXISTS keyword_id UUID REFERENCES keywords(id);
            """);
        Execute(connection, """
            CREATE TABLE IF NOT EXISTS standards (
                id UUID NOT NULL PRIMARY KEY,
                display_id TEXT NOT NULL,
                enterprise_id UUID NOT NULL REFERENCES enterprises(id),
                project_id UUID REFERENCES projects(id),
                title TEXT NOT NULL,
                description TEXT,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_standards_enterprise_id_display_id ON standards (enterprise_id, display_id);
            """);
        Execute(connection, """
            CREATE TABLE IF NOT EXISTS issues (
                id UUID NOT NULL PRIMARY KEY,
                display_id TEXT NOT NULL,
                project_id UUID NOT NULL REFERENCES projects(id),
                title TEXT NOT NULL,
                description TEXT,
                state INTEGER NOT NULL,
                resource_id UUID REFERENCES resources(id),
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_issues_project_id_display_id ON issues (project_id, display_id);
            """);
        Execute(connection, """
            CREATE TABLE IF NOT EXISTS domains (
                id UUID NOT NULL PRIMARY KEY,
                display_id TEXT NOT NULL,
                enterprise_id UUID NOT NULL REFERENCES enterprises(id),
                name TEXT NOT NULL,
                description TEXT,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_domains_enterprise_id_display_id ON domains (enterprise_id, display_id);
            """);
        Execute(connection, """
            CREATE TABLE IF NOT EXISTS systems (
                id UUID NOT NULL PRIMARY KEY,
                display_id TEXT NOT NULL,
                enterprise_id UUID NOT NULL REFERENCES enterprises(id),
                name TEXT NOT NULL,
                description TEXT,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_systems_enterprise_id_display_id ON systems (enterprise_id, display_id);
            """);
        Execute(connection, """
            CREATE TABLE IF NOT EXISTS assets (
                id UUID NOT NULL PRIMARY KEY,
                display_id TEXT NOT NULL,
                enterprise_id UUID NOT NULL REFERENCES enterprises(id),
                name TEXT NOT NULL,
                description TEXT,
                asset_type TEXT,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_assets_enterprise_id_display_id ON assets (enterprise_id, display_id);
            """);
    }

    private static void RunAddProjectResources(System.Data.Common.DbConnection connection)
    {
        Execute(connection, """
            CREATE TABLE IF NOT EXISTS project_resources (
                project_id UUID NOT NULL,
                resource_id UUID NOT NULL,
                PRIMARY KEY (project_id, resource_id),
                CONSTRAINT fk_project_resources_project FOREIGN KEY (project_id) REFERENCES projects (id) ON DELETE CASCADE,
                CONSTRAINT fk_project_resources_resource FOREIGN KEY (resource_id) REFERENCES resources (id) ON DELETE CASCADE
            );
            """);
    }

    private static void RunSeedFunWasHad(System.Data.Common.DbConnection connection)
    {
        Execute(connection, """
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
            SELECT '6c2fb7d0-4d16-4b69-b8a2-1d8e765f6c75', CONCAT(target_enterprise.display_id, '-P001'), target_enterprise.id, 'FunWasHad', 'Seed project for local development.', 0, NULL, NOW(), NOW()
            FROM target_enterprise
            WHERE NOT EXISTS (SELECT 1 FROM projects p WHERE p.enterprise_id = target_enterprise.id AND p.name = 'FunWasHad');
            """);
        Execute(connection, """
            WITH target_enterprise AS (SELECT id, display_id FROM enterprises WHERE name = '#FunWasHad' LIMIT 1)
            INSERT INTO resources (id, display_id, enterprise_id, name, description, oauth2_sub, created_at, updated_at)
            SELECT '3a00b18e-6d3d-4a64-8d12-c3bb2cb8862a', CONCAT(target_enterprise.display_id, '-sharpninja'), target_enterprise.id, 'sharpninja', 'User account.', 'sharpninja', NOW(), NOW()
            FROM target_enterprise
            WHERE NOT EXISTS (SELECT 1 FROM resources r WHERE r.enterprise_id = target_enterprise.id AND r.name = 'sharpninja');
            """);
        Execute(connection, """
            WITH target_enterprise AS (SELECT id, display_id FROM enterprises WHERE name = '#FunWasHad' LIMIT 1)
            INSERT INTO resources (id, display_id, enterprise_id, name, description, oauth2_sub, created_at, updated_at)
            SELECT '7eafef9a-9d1e-49de-9f07-4a52f0cf2a64', CONCAT(target_enterprise.display_id, '-copilot'), target_enterprise.id, 'Copilot', 'AI agent.', 'copilot', NOW(), NOW()
            FROM target_enterprise
            WHERE NOT EXISTS (SELECT 1 FROM resources r WHERE r.enterprise_id = target_enterprise.id AND r.name = 'Copilot');
            """);
        Execute(connection, """
            WITH target_enterprise AS (SELECT id, display_id FROM enterprises WHERE name = '#FunWasHad' LIMIT 1)
            INSERT INTO resources (id, display_id, enterprise_id, name, description, oauth2_sub, created_at, updated_at)
            SELECT 'd596e9f0-3f8c-4e6e-97d4-0b2b0d0a8e8a', CONCAT(target_enterprise.display_id, '-cursor'), target_enterprise.id, 'Cursor', 'AI agent.', 'cursor', NOW(), NOW()
            FROM target_enterprise
            WHERE NOT EXISTS (SELECT 1 FROM resources r WHERE r.enterprise_id = target_enterprise.id AND r.name = 'Cursor');
            """);
        Execute(connection, """
            INSERT INTO project_resources (project_id, resource_id)
            SELECT p.id, r.id FROM projects p
            JOIN enterprises e ON e.id = p.enterprise_id AND e.name = '#FunWasHad'
            JOIN resources r ON r.enterprise_id = e.id
            WHERE p.name = 'FunWasHad'
            ON CONFLICT (project_id, resource_id) DO NOTHING;
            """);
    }

    private static void EnsureMigrationsHistory(System.Data.Common.DbConnection connection)
    {
        Execute(connection, """
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" character varying(150) NOT NULL PRIMARY KEY,
                "ProductVersion" character varying(32) NOT NULL
            );
            """);
        Execute(connection, """
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES
            ('20260209180000_InitialCreate', '8.0.0'),
            ('20260209184500_AddProjectResources', '8.0.0'),
            ('20260209185000_SeedFunWasHad', '8.0.0')
            ON CONFLICT ("MigrationId") DO NOTHING;
            """);
    }

    private static void Execute(System.Data.Common.DbConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
