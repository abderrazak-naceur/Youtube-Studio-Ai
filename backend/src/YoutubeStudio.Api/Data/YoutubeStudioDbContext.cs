using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Data;

public sealed class YoutubeStudioDbContext(DbContextOptions<YoutubeStudioDbContext> options) : DbContext(options)
{
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<VideoProject> VideoProjects => Set<VideoProject>();
    public DbSet<ProductionJob> ProductionJobs => Set<ProductionJob>();
    public DbSet<ProductionArtifact> ProductionArtifacts => Set<ProductionArtifact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Workspace>(entity =>
        {
            entity.ToTable("workspaces");
            entity.HasKey(x => x.Id).HasName("pk_workspaces");
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.Name).HasDatabaseName("ix_workspaces_name");
        });

        modelBuilder.Entity<Channel>(entity =>
        {
            entity.ToTable("channels");
            entity.HasKey(x => x.Id).HasName("pk_channels");
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Platform).HasMaxLength(50).IsRequired();
            entity.HasOne(x => x.Workspace).WithMany(x => x.Channels).HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.WorkspaceId, x.Name }).HasDatabaseName("ix_channels_workspace_id_name");
        });

        modelBuilder.Entity<Opportunity>(entity =>
        {
            entity.ToTable("opportunities");
            entity.HasKey(x => x.Id).HasName("pk_opportunities");
            entity.Property(x => x.Title).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.Property(x => x.OpportunityScore).HasPrecision(5, 2);
            entity.Property(x => x.RevenueScore).HasPrecision(5, 2);
            entity.HasOne(x => x.Workspace).WithMany(x => x.Opportunities).HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.WorkspaceId, x.Status }).HasDatabaseName("ix_opportunities_workspace_id_status");
        });

        modelBuilder.Entity<VideoProject>(entity =>
        {
            entity.ToTable("video_projects");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Prompt).HasMaxLength(10000).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(500);
            entity.Property(x => x.Script).HasMaxLength(100000);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.HasOne(x => x.Workspace).WithMany().HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Channel).WithMany().HasForeignKey(x => x.ChannelId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => new { x.WorkspaceId, x.Status });
        });

        modelBuilder.Entity<ProductionJob>(entity =>
        {
            entity.ToTable("production_jobs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(x => x.Error).HasMaxLength(4000);
            entity.Property(x => x.LastCompletedStage).HasMaxLength(50);
            entity.HasOne(x => x.VideoProject).WithMany().HasForeignKey(x => x.VideoProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.VideoProjectId, x.CreatedAtUtc });
        });

        modelBuilder.Entity<ProductionArtifact>(entity =>
        {
            entity.ToTable("production_artifacts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(x => x.ProviderAssetId).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Content).HasMaxLength(200000);
            entity.Property(x => x.MetadataJson).HasMaxLength(200000);
            entity.HasOne(x => x.VideoProject).WithMany().HasForeignKey(x => x.VideoProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.VideoProjectId, x.Type });
        });
    }
}
