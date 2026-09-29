using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using YoutubeStudio.Api.Data;

#nullable disable

namespace YoutubeStudio.Api.Migrations;

[DbContext(typeof(YoutubeStudioDbContext))]
[Migration("20260915120000_AddContentDrafts")]
public partial class AddContentDrafts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "content_drafts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                ResearchProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                Angle = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                Hook = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                Outline = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                Script = table.Column<string>(type: "character varying(200000)", maxLength: 200000, nullable: false),
                TitleCandidatesJson = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                ThumbnailConceptsJson = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                Description = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                ChaptersJson = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                TagsJson = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_content_drafts", x => x.Id);
                table.ForeignKey("FK_content_drafts_research_projects_ResearchProjectId", x => x.ResearchProjectId, "research_projects", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "ix_content_drafts_project_id", table: "content_drafts", column: "ResearchProjectId", unique: true);
        migrationBuilder.CreateIndex(name: "ix_content_drafts_workspace_id_project_id", table: "content_drafts", columns: new[] { "WorkspaceId", "ResearchProjectId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "content_drafts");
    }
}
