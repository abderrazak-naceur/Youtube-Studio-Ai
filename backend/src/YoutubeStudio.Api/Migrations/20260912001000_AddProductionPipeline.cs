using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using YoutubeStudio.Api.Data;

#nullable disable

namespace YoutubeStudio.Api.Migrations;

[DbContext(typeof(YoutubeStudioDbContext))]
[Migration("20260912001000_AddProductionPipeline")]
public partial class AddProductionPipeline : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "production_jobs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                VideoProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Attempt = table.Column<int>(type: "integer", nullable: false),
                Error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                LastCompletedStage = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_production_jobs", x => x.Id);
                table.ForeignKey("FK_production_jobs_video_projects_VideoProjectId", x => x.VideoProjectId, "video_projects", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "production_artifacts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                VideoProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                ProviderAssetId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Content = table.Column<string>(type: "character varying(200000)", maxLength: 200000, nullable: true),
                MetadataJson = table.Column<string>(type: "character varying(200000)", maxLength: 200000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_production_artifacts", x => x.Id);
                table.ForeignKey("FK_production_artifacts_video_projects_VideoProjectId", x => x.VideoProjectId, "video_projects", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_production_jobs_VideoProjectId_CreatedAtUtc",
            table: "production_jobs",
            columns: new[] { "VideoProjectId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_production_artifacts_VideoProjectId_Type",
            table: "production_artifacts",
            columns: new[] { "VideoProjectId", "Type" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "production_artifacts");
        migrationBuilder.DropTable(name: "production_jobs");
    }
}
