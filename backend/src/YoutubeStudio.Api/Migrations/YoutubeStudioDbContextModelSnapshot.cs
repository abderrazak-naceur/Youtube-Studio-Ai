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

        modelBuilder.Entity("YoutubeStudio.Api.Models.VideoProject", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
            b.Property<Guid?>("ChannelId").HasColumnType("uuid").HasColumnName("channel_id");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<string>("Prompt").IsRequired().HasMaxLength(10000).HasColumnName("prompt");
            b.Property<string>("Script").HasMaxLength(100000).HasColumnName("script");
            b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnName("status");
            b.Property<string>("Title").HasMaxLength(500).HasColumnName("title");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("updated_at_utc");
            b.Property<Guid>("WorkspaceId").HasColumnType("uuid").HasColumnName("workspace_id");
            b.HasKey("Id").HasName("PK_video_projects");
            b.HasIndex("ChannelId").HasDatabaseName("IX_video_projects_ChannelId");
            b.HasIndex("WorkspaceId", "Status").HasDatabaseName("IX_video_projects_WorkspaceId_Status");
            b.ToTable("video_projects");
        });

        modelBuilder.Entity("YoutubeStudio.Api.Models.ProductionJob", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
            b.Property<int>("Attempt").HasColumnType("integer").HasColumnName("attempt");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<string>("Error").HasMaxLength(4000).HasColumnName("error");
            b.Property<string>("LastCompletedStage").HasMaxLength(50).HasColumnName("last_completed_stage");
            b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnName("status");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("updated_at_utc");
            b.Property<Guid>("VideoProjectId").HasColumnType("uuid").HasColumnName("video_project_id");
            b.HasKey("Id").HasName("PK_production_jobs");
            b.HasIndex("VideoProjectId", "CreatedAtUtc").HasDatabaseName("IX_production_jobs_VideoProjectId_CreatedAtUtc");
            b.ToTable("production_jobs");
        });

        modelBuilder.Entity("YoutubeStudio.Api.Models.ProductionArtifact", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
            b.Property<string>("Content").HasMaxLength(200000).HasColumnName("content");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<string>("MetadataJson").HasMaxLength(200000).HasColumnName("metadata_json");
            b.Property<string>("ProviderAssetId").IsRequired().HasMaxLength(500).HasColumnName("provider_asset_id");
            b.Property<string>("Type").IsRequired().HasMaxLength(50).HasColumnName("type");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("updated_at_utc");
            b.Property<Guid>("VideoProjectId").HasColumnType("uuid").HasColumnName("video_project_id");
            b.HasKey("Id").HasName("PK_production_artifacts");
            b.HasIndex("VideoProjectId", "Type").HasDatabaseName("IX_production_artifacts_VideoProjectId_Type");
            b.ToTable("production_artifacts");
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

        modelBuilder.Entity("YoutubeStudio.Api.Models.VideoProject", b =>
        {
            b.HasOne("YoutubeStudio.Api.Models.Channel", "Channel")
                .WithMany()
                .HasForeignKey("ChannelId")
                .OnDelete(DeleteBehavior.SetNull);
            b.HasOne("YoutubeStudio.Api.Models.Workspace", "Workspace")
                .WithMany()
                .HasForeignKey("WorkspaceId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("YoutubeStudio.Api.Models.ProductionJob", b =>
            b.HasOne("YoutubeStudio.Api.Models.VideoProject", "VideoProject")
                .WithMany()
                .HasForeignKey("VideoProjectId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired());

        modelBuilder.Entity("YoutubeStudio.Api.Models.ProductionArtifact", b =>
            b.HasOne("YoutubeStudio.Api.Models.VideoProject", "VideoProject")
                .WithMany()
                .HasForeignKey("VideoProjectId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired());

        modelBuilder.Entity("YoutubeStudio.Api.Models.Channel", b => b.Navigation("Workspace"));
        modelBuilder.Entity("YoutubeStudio.Api.Models.Opportunity", b => b.Navigation("Workspace"));
        modelBuilder.Entity("YoutubeStudio.Api.Models.VideoProject", b =>
        {
            b.Navigation("Channel");
            b.Navigation("Workspace");
        });
        modelBuilder.Entity("YoutubeStudio.Api.Models.ProductionJob", b => b.Navigation("VideoProject"));
        modelBuilder.Entity("YoutubeStudio.Api.Models.ProductionArtifact", b => b.Navigation("VideoProject"));
        modelBuilder.Entity("YoutubeStudio.Api.Models.Workspace", b =>
        {
            b.Navigation("Channels");
            b.Navigation("Opportunities");
        });
#pragma warning restore 612, 618
    }
}
