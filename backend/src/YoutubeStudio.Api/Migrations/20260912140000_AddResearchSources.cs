using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using YoutubeStudio.Api.Data;

#nullable disable

namespace YoutubeStudio.Api.Migrations;

[DbContext(typeof(YoutubeStudioDbContext))]
[Migration("20260912140000_AddResearchSources")]
public partial class AddResearchSources : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "research_sources",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                ResearchProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                MetadataJson = table.Column<string>(type: "character varying(200000)", maxLength: 200000, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_research_sources", x => x.Id);
                table.ForeignKey(
                    name: "FK_research_sources_research_projects_ResearchProjectId",
                    column: x => x.ResearchProjectId,
                    principalTable: "research_projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_research_sources_project_id_created_at",
            table: "research_sources",
            columns: new[] { "ResearchProjectId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "ix_research_sources_workspace_id_project_id",
            table: "research_sources",
            columns: new[] { "WorkspaceId", "ResearchProjectId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "research_sources");
    }
}
