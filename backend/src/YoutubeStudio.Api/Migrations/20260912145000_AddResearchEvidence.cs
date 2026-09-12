using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeStudio.Api.Migrations;

public partial class AddResearchEvidence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "research_evidence",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                ResearchSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                Quote = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                Locator = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Context = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                MetadataJson = table.Column<string>(type: "character varying(200000)", maxLength: 200000, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_research_evidence", x => x.Id);
                table.ForeignKey("FK_research_evidence_research_sources_ResearchSourceId", x => x.ResearchSourceId, "research_sources", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "ix_research_evidence_source_id_created_at", table: "research_evidence", columns: new[] { "ResearchSourceId", "CreatedAtUtc" });
        migrationBuilder.CreateIndex(name: "ix_research_evidence_workspace_id_source_id", table: "research_evidence", columns: new[] { "WorkspaceId", "ResearchSourceId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "research_evidence");
}
