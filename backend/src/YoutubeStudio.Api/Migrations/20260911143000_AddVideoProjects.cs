using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using YoutubeStudio.Api.Data;

#nullable disable

namespace YoutubeStudio.Api.Migrations;

[DbContext(typeof(YoutubeStudioDbContext))]
[Migration("20260911143000_AddVideoProjects")]
public partial class AddVideoProjects : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "video_projects",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                ChannelId = table.Column<Guid>(type: "uuid", nullable: true),
                Prompt = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Script = table.Column<string>(type: "character varying(100000)", maxLength: 100000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_video_projects", x => x.Id);
                table.ForeignKey("FK_video_projects_channels_ChannelId", x => x.ChannelId, "channels", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_video_projects_workspaces_WorkspaceId", x => x.WorkspaceId, "workspaces", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_video_projects_ChannelId",
            table: "video_projects",
            column: "ChannelId");

        migrationBuilder.CreateIndex(
            name: "IX_video_projects_WorkspaceId_Status",
            table: "video_projects",
            columns: new[] { "WorkspaceId", "Status" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "video_projects");
    }
}
