using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeStudio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddArtifactVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_production_artifacts_VideoProjectId_Type",
                table: "production_artifacts");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "production_artifacts",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_production_artifacts_VideoProjectId_Type_Version",
                table: "production_artifacts",
                columns: new[] { "VideoProjectId", "Type", "Version" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_production_artifacts_VideoProjectId_Type_Version",
                table: "production_artifacts");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "production_artifacts");

            migrationBuilder.CreateIndex(
                name: "IX_production_artifacts_VideoProjectId_Type",
                table: "production_artifacts",
                columns: new[] { "VideoProjectId", "Type" });
        }
    }
}
