using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeStudio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelDna : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "channel_dna",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Audience = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Positioning = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Tone = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    VisualLanguage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RecurringFormats = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    ForbiddenPatterns = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    StrategicGoals = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_channel_dna", x => x.Id);
                    table.ForeignKey(
                        name: "FK_channel_dna_channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_channel_dna_channel_id_version",
                table: "channel_dna",
                columns: new[] { "ChannelId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "channel_dna");
        }
    }
}
