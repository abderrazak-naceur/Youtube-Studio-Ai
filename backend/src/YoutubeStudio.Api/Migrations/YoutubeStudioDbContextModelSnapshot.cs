using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
        modelBuilder.HasAnnotation("Relational:MaxIdentifierLength", 63);
        modelBuilder.HasAnnotation("Npgsql:PostgresModelCustomizer:PostgresVersion", new Version(17, 0));
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity("YoutubeStudio.Api.Models.Workspace", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone");
            b.HasKey("Id").HasName("pk_workspaces");
            b.HasIndex("Name").HasDatabaseName("ix_workspaces_name");
            b.ToTable("workspaces");
        });

        modelBuilder.Entity("YoutubeStudio.Api.Models.Channel", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("ExternalChannelId").HasColumnType("text");
            b.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<string>("Platform").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<Guid>("WorkspaceId").HasColumnType("uuid");
            b.HasKey("Id").HasName("pk_channels");
            b.HasIndex("WorkspaceId", "Name").HasDatabaseName("ix_channels_workspace_id_name");
            b.ToTable("channels");
        });

        modelBuilder.Entity("YoutubeStudio.Api.Models.Opportunity", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<string>("AudienceProblem").HasColumnType("text");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<decimal>("OpportunityScore").HasPrecision(5, 2).HasColumnType("numeric(5,2)");
            b.Property<decimal>("RevenueScore").HasPrecision(5, 2).HasColumnType("numeric(5,2)");
            b.Property<string>("Rationale").HasColumnType("text");
            b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<string>("Title").IsRequired().HasMaxLength(500).HasColumnType("character varying(500)");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<Guid>("WorkspaceId").HasColumnType("uuid");
            b.HasKey("Id").HasName("pk_opportunities");
            b.HasIndex("WorkspaceId", "Status").HasDatabaseName("ix_opportunities_workspace_id_status");
            b.ToTable("opportunities");
        });

        modelBuilder.Entity("YoutubeStudio.Api.Models.VideoProject", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<Guid?>("ChannelId").HasColumnType("uuid");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("Prompt").IsRequired().HasMaxLength(10000).HasColumnType("character varying(10000)");
            b.Property<string>("Script").HasMaxLength(100000).HasColumnType("character varying(100000)");
            b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<string>("Title").HasMaxLength(500).HasColumnType("character varying(500)");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<Guid>("WorkspaceId").HasColumnType("uuid");
            b.HasKey("Id").HasName("PK_video_projects");
            b.HasIndex("ChannelId").HasDatabaseName("IX_video_projects_ChannelId");
            b.HasIndex("WorkspaceId", "Status").HasDatabaseName("IX_video_projects_WorkspaceId_Status");
            b.ToTable("video_projects");
        });

        modelBuilder.Entity("YoutubeStudio.Api.Models.ProductionJob", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<int>("Attempt").HasColumnType("integer");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("Error").HasMaxLength(4000).HasColumnType("character varying(4000)");
            b.Property<string>("LastCompletedStage").HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<Guid>("VideoProjectId").HasColumnType("uuid");
            b.HasKey("Id").HasName("PK_production_jobs");
            b.HasIndex("VideoProjectId", "CreatedAtUtc").HasDatabaseName("IX_production_jobs_VideoProjectId_CreatedAtUtc");
            b.ToTable("production_jobs");
        });

        modelBuilder.Entity("YoutubeStudio.Api.Models.ProductionArtifact", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<string>("Content").HasMaxLength(200000).HasColumnType("character varying(200000)");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("MetadataJson").HasMaxLength(200000).HasColumnType("character varying(200000)");
            b.Property<string>("ProviderAssetId").IsRequired().HasMaxLength(500).HasColumnType("character varying(500)");
            b.Property<string>("Type").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<Guid>("VideoProjectId").HasColumnType("uuid");
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
