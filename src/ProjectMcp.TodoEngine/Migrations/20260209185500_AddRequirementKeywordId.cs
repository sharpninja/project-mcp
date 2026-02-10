using Microsoft.EntityFrameworkCore.Migrations;

namespace ProjectMCP.TodoEngine.Migrations;

/// <summary>Adds requirement-keyword link: requirements.keyword_id FK to keywords(id).</summary>
[Migration("20260209185500_AddRequirementKeywordId")]
public partial class AddRequirementKeywordId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
ALTER TABLE requirements ADD COLUMN IF NOT EXISTS keyword_id UUID REFERENCES keywords(id);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE requirements DROP COLUMN IF EXISTS keyword_id;");
    }
}
