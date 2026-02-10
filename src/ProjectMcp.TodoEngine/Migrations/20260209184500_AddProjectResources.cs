using Microsoft.EntityFrameworkCore.Migrations;

namespace ProjectMCP.TodoEngine.Migrations;

[Migration("20260209184500_AddProjectResources")]
public partial class AddProjectResources : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
CREATE TABLE IF NOT EXISTS project_resources (
    project_id UUID NOT NULL,
    resource_id UUID NOT NULL,
    PRIMARY KEY (project_id, resource_id),
    CONSTRAINT fk_project_resources_project FOREIGN KEY (project_id) REFERENCES projects (id) ON DELETE CASCADE,
    CONSTRAINT fk_project_resources_resource FOREIGN KEY (resource_id) REFERENCES resources (id) ON DELETE CASCADE
);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS project_resources;");
    }
}
