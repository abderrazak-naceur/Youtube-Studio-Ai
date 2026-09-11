using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using YoutubeStudio.Api.Data;

#nullable disable

namespace YoutubeStudio.Api.Migrations;

[DbContext(typeof(YoutubeStudioDbContext))]
partial class YoutubeStudioDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0");
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity("YoutubeStudio.Api.Models.Workspace", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnName("name");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("updated_at_utc");
            b.HasKey("Id").HasName("pk_workspaces");
            b.HasIndex("Name").HasDatabaseName("ix_workspaces_name");
            b.ToTable("workspaces");
        });

        modelBuilder.Entity("YoutubeStudio.Api.Models.Channel", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<string>("ExternalChannelId").HasColumnType("text").HasColumnName("external_channel_id");
            b.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnName("name");
            b.Property<string>("Platform").IsRequired().HasMaxLength(50).HasColumnName("platform");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("updated_at_utc");
            b.Property<Guid>("WorkspaceId").HasColumnType("uuid").HasColumnName("workspace_id");
            b.HasKey("Id").HasName("pk_channels");
            b.HasIndex("WorkspaceId", "Name").HasDatabaseName("ix_channels_workspace_id_name");
            b.ToTable("channels");
        });

        modelBuilder.Entity("YoutubeStudio.Api.Models.Opportunity", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
            b.Property<string>("AudienceProblem").HasColumnType("text").HasColumnName("audience_problem");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<decimal>("OpportunityScore").HasPrecision(5, 2).HasColumnType("numeric(5,2)").HasColumnName("opportunity_score");
            b.Property<string>("Rationale").HasColumnType("text").HasColumnName("rationale");
            b.Property<decimal>("RevenueScore").HasPrecision(5, 2).HasColumnType("numeric(5,2)").HasColumnName("revenue_score");
            b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnName("status");
            b.Property<string>("Title").IsRequired().HasMaxLength(500).HasColumnName("title");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("updated_at_utc");
            b.Property<Guid>("WorkspaceId").HasColumnType("uuid").HasColumnName("workspace_id");
            b.HasKey("Id").HasName("pk_opportunities");
            b.HasIndex("WorkspaceId", "Status").HasDatabaseName("ix_opportunities_workspace_id_status");
            b.ToTable("opportunities");
        });

        modelBuilder.Entity("YoutubeStudio.Api.Models.Channel", b =>
            b.HasOne("YoutubeStudio.Api.Models.Workspace", "Workspace")
                .WithMany("Channels")
                .HasForeignKey("WorkspaceId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired());

        modelBuilder.Entity("YoutubeStudio.Api.Models.Opportunity", b =>
            b.HasOne("YoutubeStudio.Api.Models.Workspace", "Workspace")
                .WithMany("Opportunities")
                .HasForeignKey("WorkspaceId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired());

        modelBuilder.Entity("YoutubeStudio.Api.Models.Channel", b => b.Navigation("Workspace"));
        modelBuilder.Entity("YoutubeStudio.Api.Models.Opportunity", b => b.Navigation("Workspace"));
        modelBuilder.Entity("YoutubeStudio.Api.Models.Workspace", b =>
        {
            b.Navigation("Channels");
            b.Navigation("Opportunities");
        });
#pragma warning restore 612, 618
    }
}
