using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeStudio.Api.Migrations;

public partial class AddResearchClaimProjectIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_research_claims_ResearchProjectId",
            table: "research_claims",
            column: "ResearchProjectId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_research_claims_ResearchProjectId",
            table: "research_claims");
    }
}
