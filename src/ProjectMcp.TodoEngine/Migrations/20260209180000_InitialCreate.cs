using Microsoft.EntityFrameworkCore.Migrations;

namespace ProjectMCP.TodoEngine.Migrations;

/// <summary>Creates base tables for the TodoEngine. Must run before AddProjectResources and SeedFunWasHad.</summary>
[Migration("20260209180000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
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

        migrationBuilder.Sql("""
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

        migrationBuilder.Sql("""
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

        migrationBuilder.Sql("""
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

        migrationBuilder.Sql("""
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

        migrationBuilder.Sql("""
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

        migrationBuilder.Sql("""
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

        migrationBuilder.Sql("""
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

        migrationBuilder.Sql("""
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

        migrationBuilder.Sql("""
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

        migrationBuilder.Sql("""
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

        migrationBuilder.Sql("""
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

        migrationBuilder.Sql("""
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

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS assets;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS systems;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS domains;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS keywords;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS issues;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS standards;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS requirements;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS releases;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS milestones;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS work_items;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS resources;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS projects;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS enterprises;");
    }
}
