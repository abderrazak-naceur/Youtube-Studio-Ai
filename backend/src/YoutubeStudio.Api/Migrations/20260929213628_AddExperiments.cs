using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeStudio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExperiments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "experiments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    Variable = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Hypothesis = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SuccessMetric = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BaselineValue = table.Column<double>(type: "double precision", nullable: false),
                    DecisionRule = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResultValue = table.Column<double>(type: "double precision", nullable: true),
                    Outcome = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experiments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_experiments_WorkspaceId_CreatedAtUtc",
                table: "experiments",
                columns: new[] { "WorkspaceId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "experiments");
        }
    }
}
