using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using YoutubeStudio.Api.Data;

#nullable disable

namespace YoutubeStudio.Api.Migrations;

[DbContext(typeof(YoutubeStudioDbContext))]
[Migration("20260916120000_AddVideoProjectContentLink")]
public partial class AddVideoProjectContentLink : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ContentDraftId",
            table: "video_projects",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Source",
            table: "video_projects",
            type: "character varying(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "prompt");

        migrationBuilder.CreateIndex(
            name: "IX_video_projects_ContentDraftId",
            table: "video_projects",
            column: "ContentDraftId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_video_projects_ContentDraftId", table: "video_projects");
        migrationBuilder.DropColumn(name: "Source", table: "video_projects");
        migrationBuilder.DropColumn(name: "ContentDraftId", table: "video_projects");
    }
}
