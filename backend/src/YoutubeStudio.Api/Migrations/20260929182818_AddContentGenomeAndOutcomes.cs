using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeStudio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddContentGenomeAndOutcomes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_genomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    SceneCount = table.Column<int>(type: "integer", nullable: false),
                    WordCount = table.Column<int>(type: "integer", nullable: false),
                    AttributesJson = table.Column<string>(type: "character varying(200000)", maxLength: 200000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_genomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_genomes_video_projects_VideoProjectId",
                        column: x => x.VideoProjectId,
                        principalTable: "video_projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "video_outcomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MeasuredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Views = table.Column<long>(type: "bigint", nullable: false),
                    Likes = table.Column<long>(type: "bigint", nullable: false),
                    Comments = table.Column<long>(type: "bigint", nullable: false),
                    AverageViewDurationSeconds = table.Column<double>(type: "double precision", nullable: false),
                    EstimatedRevenueUsd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_outcomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_video_outcomes_video_projects_VideoProjectId",
                        column: x => x.VideoProjectId,
                        principalTable: "video_projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_content_genomes_VideoProjectId",
                table: "content_genomes",
                column: "VideoProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_video_outcomes_VideoProjectId_MeasuredAtUtc",
                table: "video_outcomes",
                columns: new[] { "VideoProjectId", "MeasuredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_genomes");

            migrationBuilder.DropTable(
                name: "video_outcomes");
        }
    }
}
