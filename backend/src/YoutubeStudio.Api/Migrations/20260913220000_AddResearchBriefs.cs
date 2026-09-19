using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeStudio.Api.Migrations;

[Migration("20260913220000_AddResearchBriefs")]
public partial class AddResearchBriefs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "research_briefs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                ResearchProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                Markdown = table.Column<string>(type: "character varying(200000)", maxLength: 200000, nullable: false),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                PendingClaimCount = table.Column<int>(type: "integer", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_research_briefs", x => x.Id);
                table.ForeignKey("FK_research_briefs_research_projects_ResearchProjectId", x => x.ResearchProjectId, "research_projects", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "ix_research_briefs_project_id", table: "research_briefs", column: "ResearchProjectId", unique: true);
        migrationBuilder.CreateIndex(name: "ix_research_briefs_workspace_id_project_id", table: "research_briefs", columns: new[] { "WorkspaceId", "ResearchProjectId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "research_briefs");
    }
}
