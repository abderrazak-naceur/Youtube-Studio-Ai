using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Data;

public sealed class YoutubeStudioDbContext(DbContextOptions<YoutubeStudioDbContext> options) : DbContext(options)
{
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Workspace>(entity =>
        {
            entity.ToTable("workspaces");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<Channel>(entity =>
        {
            entity.ToTable("channels");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Platform).HasMaxLength(50).IsRequired();
            entity.HasOne(x => x.Workspace)
                .WithMany(x => x.Channels)
                .HasForeignKey(x => x.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.WorkspaceId, x.Name });
        });

        modelBuilder.Entity<Opportunity>(entity =>
        {
            entity.ToTable("opportunities");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.Property(x => x.OpportunityScore).HasPrecision(5, 2);
            entity.Property(x => x.RevenueScore).HasPrecision(5, 2);
            entity.HasOne(x => x.Workspace)
                .WithMany(x => x.Opportunities)
                .HasForeignKey(x => x.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.WorkspaceId, x.Status });
        });
    }
}
