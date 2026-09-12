using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeStudio.Api.Migrations;

public partial class AddResearchClaims : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "research_claims",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                ResearchProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                Text = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                VerificationStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                MetadataJson = table.Column<string>(type: "character varying(200000)", maxLength: 200000, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_research_claims", x => x.Id);
                table.ForeignKey("FK_research_claims_research_projects_ResearchProjectId", x => x.ResearchProjectId, "research_projects", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "research_claim_evidence",
            columns: table => new
            {
                ResearchClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                ResearchEvidenceId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_research_claim_evidence", x => new { x.ResearchClaimId, x.ResearchEvidenceId });
                table.ForeignKey("FK_research_claim_evidence_research_claims_ResearchClaimId", x => x.ResearchClaimId, "research_claims", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_research_claim_evidence_research_evidence_ResearchEvidenceId", x => x.ResearchEvidenceId, "research_evidence", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "ix_research_claims_workspace_id_project_id", table: "research_claims", columns: new[] { "WorkspaceId", "ResearchProjectId" });
        migrationBuilder.CreateIndex(name: "ix_research_claim_evidence_evidence_id", table: "research_claim_evidence", column: "ResearchEvidenceId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "research_claim_evidence");
        migrationBuilder.DropTable(name: "research_claims");
    }
}
