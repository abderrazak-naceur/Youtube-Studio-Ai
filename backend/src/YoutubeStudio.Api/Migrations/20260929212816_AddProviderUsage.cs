using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeStudio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "provider_usage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    VideoProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    Provider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TaskType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InputUnits = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    OutputUnits = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPriceUsd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalCostUsd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_usage", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_provider_usage_Provider",
                table: "provider_usage",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_provider_usage_VideoProjectId_CreatedAtUtc",
                table: "provider_usage",
                columns: new[] { "VideoProjectId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_usage_WorkspaceId_CreatedAtUtc",
                table: "provider_usage",
                columns: new[] { "WorkspaceId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "provider_usage");
        }
    }
}
