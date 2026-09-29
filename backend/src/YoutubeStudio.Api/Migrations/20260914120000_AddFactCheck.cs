using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using YoutubeStudio.Api.Data;

#nullable disable

namespace YoutubeStudio.Api.Migrations;

[DbContext(typeof(YoutubeStudioDbContext))]
[Migration("20260914120000_AddFactCheck")]
public partial class AddFactCheck : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "fact_check_reports",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                ResearchProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                Verdict = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                ClaimCount = table.Column<int>(type: "integer", nullable: false),
                VerifiedClaimCount = table.Column<int>(type: "integer", nullable: false),
                DisputedClaimCount = table.Column<int>(type: "integer", nullable: false),
                UnverifiedClaimCount = table.Column<int>(type: "integer", nullable: false),
                UnsupportedClaimCount = table.Column<int>(type: "integer", nullable: false),
                RequiresHumanReview = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_fact_check_reports", x => x.Id);
                table.ForeignKey("FK_fact_check_reports_research_projects_ResearchProjectId", x => x.ResearchProjectId, "research_projects", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "fact_check_findings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                FactCheckReportId = table.Column<Guid>(type: "uuid", nullable: false),
                ResearchClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                RiskLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                EvidenceCount = table.Column<int>(type: "integer", nullable: false),
                RequiresHumanReview = table.Column<bool>(type: "boolean", nullable: false),
                Rationale = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_fact_check_findings", x => x.Id);
                table.ForeignKey("FK_fact_check_findings_fact_check_reports_FactCheckReportId", x => x.FactCheckReportId, "fact_check_reports", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_fact_check_findings_research_claims_ResearchClaimId", x => x.ResearchClaimId, "research_claims", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "ix_fact_check_reports_project_id", table: "fact_check_reports", column: "ResearchProjectId", unique: true);
        migrationBuilder.CreateIndex(name: "ix_fact_check_reports_workspace_id_project_id", table: "fact_check_reports", columns: new[] { "WorkspaceId", "ResearchProjectId" });
        migrationBuilder.CreateIndex(name: "ix_fact_check_findings_report_id", table: "fact_check_findings", column: "FactCheckReportId");
        migrationBuilder.CreateIndex(name: "ix_fact_check_findings_claim_id", table: "fact_check_findings", column: "ResearchClaimId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "fact_check_findings");
        migrationBuilder.DropTable(name: "fact_check_reports");
    }
}
