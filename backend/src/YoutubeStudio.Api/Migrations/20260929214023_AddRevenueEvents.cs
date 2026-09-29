using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeStudio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRevenueEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "revenue_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: true),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AmountUsd = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Cta = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AttributionConfidence = table.Column<double>(type: "double precision", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_revenue_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_revenue_events_VideoProjectId_OccurredAtUtc",
                table: "revenue_events",
                columns: new[] { "VideoProjectId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_revenue_events_WorkspaceId_OccurredAtUtc",
                table: "revenue_events",
                columns: new[] { "WorkspaceId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "revenue_events");
        }
    }
}
