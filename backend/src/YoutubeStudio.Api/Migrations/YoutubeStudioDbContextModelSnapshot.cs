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
            b.Property<Guid>("Id").HasColumnType("uuid"); b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)"); b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone"); b.HasKey("Id").HasName("pk_workspaces"); b.HasIndex("Name").HasDatabaseName("ix_workspaces_name"); b.ToTable("workspaces");
        });
        modelBuilder.Entity("YoutubeStudio.Api.Models.Channel", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid"); b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<string>("ExternalChannelId").HasColumnType("text"); b.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)"); b.Property<string>("Platform").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)"); b.Property<Guid>("WorkspaceId").HasColumnType("uuid"); b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone"); b.HasKey("Id").HasName("pk_channels"); b.HasIndex("WorkspaceId", "Name").HasDatabaseName("ix_channels_workspace_id_name"); b.ToTable("channels");
        });
        modelBuilder.Entity("YoutubeStudio.Api.Models.Opportunity", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid"); b.Property<string>("AudienceProblem").HasColumnType("text"); b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<decimal>("OpportunityScore").HasPrecision(5, 2).HasColumnType("numeric(5,2)"); b.Property<decimal>("RevenueScore").HasPrecision(5, 2).HasColumnType("numeric(5,2)"); b.Property<string>("Rationale").HasColumnType("text"); b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)"); b.Property<string>("Title").IsRequired().HasMaxLength(500).HasColumnType("character varying(500)"); b.Property<Guid>("WorkspaceId").HasColumnType("uuid"); b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone"); b.HasKey("Id").HasName("pk_opportunities"); b.HasIndex("WorkspaceId", "Status").HasDatabaseName("ix_opportunities_workspace_id_status"); b.ToTable("opportunities");
        });
        modelBuilder.Entity("YoutubeStudio.Api.Models.ResearchProject", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid"); b.Property<Guid>("WorkspaceId").HasColumnType("uuid"); b.Property<Guid>("OpportunityId").HasColumnType("uuid"); b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)"); b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone"); b.HasKey("Id").HasName("pk_research_projects"); b.HasIndex("OpportunityId").IsUnique(); b.HasIndex("WorkspaceId", "CreatedAtUtc").HasDatabaseName("ix_research_projects_workspace_id_created_at"); b.ToTable("research_projects");
        });
        modelBuilder.Entity("YoutubeStudio.Api.Models.ResearchSource", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid"); b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<string>("MetadataJson").IsRequired().HasMaxLength(200000).HasColumnType("character varying(200000)"); b.Property<Guid>("ResearchProjectId").HasColumnType("uuid"); b.Property<string>("Title").IsRequired().HasMaxLength(500).HasColumnType("character varying(500)"); b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<string>("Url").IsRequired().HasMaxLength(2048).HasColumnType("character varying(2048)"); b.Property<Guid>("WorkspaceId").HasColumnType("uuid"); b.HasKey("Id").HasName("pk_research_sources"); b.HasIndex("ResearchProjectId", "CreatedAtUtc").HasDatabaseName("ix_research_sources_project_id_created_at"); b.HasIndex("WorkspaceId", "ResearchProjectId").HasDatabaseName("ix_research_sources_workspace_id_project_id"); b.ToTable("research_sources");
        });
        modelBuilder.Entity("YoutubeStudio.Api.Models.ResearchEvidence", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid"); b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<string>("Context").HasMaxLength(10000).HasColumnType("character varying(10000)"); b.Property<string>("Locator").HasMaxLength(1000).HasColumnType("character varying(1000)"); b.Property<string>("MetadataJson").IsRequired().HasMaxLength(200000).HasColumnType("character varying(200000)"); b.Property<string>("Quote").IsRequired().HasMaxLength(20000).HasColumnType("character varying(20000)"); b.Property<Guid>("ResearchSourceId").HasColumnType("uuid"); b.Property<Guid>("WorkspaceId").HasColumnType("uuid"); b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone"); b.HasKey("Id").HasName("pk_research_evidence"); b.HasIndex("ResearchSourceId", "CreatedAtUtc").HasDatabaseName("ix_research_evidence_source_id_created_at"); b.HasIndex("WorkspaceId", "ResearchSourceId").HasDatabaseName("ix_research_evidence_workspace_id_source_id"); b.ToTable("research_evidence");
        });
        modelBuilder.Entity("YoutubeStudio.Api.Models.ResearchClaim", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid"); b.Property<Guid>("WorkspaceId").HasColumnType("uuid"); b.Property<Guid>("ResearchProjectId").HasColumnType("uuid"); b.Property<string>("Text").IsRequired().HasMaxLength(20000).HasColumnType("character varying(20000)"); b.Property<string>("VerificationStatus").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)"); b.Property<string>("MetadataJson").IsRequired().HasMaxLength(200000).HasColumnType("character varying(200000)"); b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone"); b.HasKey("Id").HasName("pk_research_claims"); b.HasIndex("WorkspaceId", "ResearchProjectId").HasDatabaseName("ix_research_claims_workspace_id_project_id"); b.HasIndex("ResearchProjectId").HasDatabaseName("IX_research_claims_ResearchProjectId"); b.ToTable("research_claims");
        });
        modelBuilder.Entity("YoutubeStudio.Api.Models.ResearchClaimEvidence", b =>
        {
            b.Property<Guid>("ResearchClaimId").HasColumnType("uuid"); b.Property<Guid>("ResearchEvidenceId").HasColumnType("uuid"); b.HasKey("ResearchClaimId", "ResearchEvidenceId").HasName("pk_research_claim_evidence"); b.HasIndex("ResearchEvidenceId").HasDatabaseName("ix_research_claim_evidence_evidence_id"); b.ToTable("research_claim_evidence");
        });
        modelBuilder.Entity("YoutubeStudio.Api.Models.ResearchBrief", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid"); b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<string>("Markdown").IsRequired().HasMaxLength(200000).HasColumnType("character varying(200000)"); b.Property<int>("PendingClaimCount").HasColumnType("integer"); b.Property<Guid>("ResearchProjectId").HasColumnType("uuid"); b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)"); b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<Guid>("WorkspaceId").HasColumnType("uuid"); b.HasKey("Id").HasName("pk_research_briefs"); b.HasIndex("ResearchProjectId").IsUnique().HasDatabaseName("ix_research_briefs_project_id"); b.HasIndex("WorkspaceId", "ResearchProjectId").HasDatabaseName("ix_research_briefs_workspace_id_project_id"); b.ToTable("research_briefs");
        });
        modelBuilder.Entity("YoutubeStudio.Api.Models.FactCheckReport", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid"); b.Property<int>("ClaimCount").HasColumnType("integer"); b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<int>("DisputedClaimCount").HasColumnType("integer"); b.Property<bool>("RequiresHumanReview").HasColumnType("boolean"); b.Property<Guid>("ResearchProjectId").HasColumnType("uuid"); b.Property<int>("UnsupportedClaimCount").HasColumnType("integer"); b.Property<int>("UnverifiedClaimCount").HasColumnType("integer"); b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<string>("Verdict").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)"); b.Property<int>("VerifiedClaimCount").HasColumnType("integer"); b.Property<Guid>("WorkspaceId").HasColumnType("uuid"); b.HasKey("Id").HasName("pk_fact_check_reports"); b.HasIndex("ResearchProjectId").IsUnique().HasDatabaseName("ix_fact_check_reports_project_id"); b.HasIndex("WorkspaceId", "ResearchProjectId").HasDatabaseName("ix_fact_check_reports_workspace_id_project_id"); b.ToTable("fact_check_reports");
        });
        modelBuilder.Entity("YoutubeStudio.Api.Models.FactCheckFinding", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid"); b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<int>("EvidenceCount").HasColumnType("integer"); b.Property<Guid>("FactCheckReportId").HasColumnType("uuid"); b.Property<string>("Rationale").IsRequired().HasMaxLength(20000).HasColumnType("character varying(20000)"); b.Property<Guid>("ResearchClaimId").HasColumnType("uuid"); b.Property<bool>("RequiresHumanReview").HasColumnType("boolean"); b.Property<string>("RiskLevel").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)"); b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)"); b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<Guid>("WorkspaceId").HasColumnType("uuid"); b.HasKey("Id").HasName("pk_fact_check_findings"); b.HasIndex("FactCheckReportId").HasDatabaseName("ix_fact_check_findings_report_id"); b.HasIndex("ResearchClaimId").HasDatabaseName("ix_fact_check_findings_claim_id"); b.ToTable("fact_check_findings");
        });
        modelBuilder.Entity("YoutubeStudio.Api.Models.ContentDraft", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid"); b.Property<string>("Angle").IsRequired().HasMaxLength(2000).HasColumnType("character varying(2000)"); b.Property<string>("ChaptersJson").IsRequired().HasMaxLength(20000).HasColumnType("character varying(20000)"); b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<string>("Description").IsRequired().HasMaxLength(20000).HasColumnType("character varying(20000)"); b.Property<string>("Hook").IsRequired().HasMaxLength(4000).HasColumnType("character varying(4000)"); b.Property<string>("Outline").IsRequired().HasMaxLength(20000).HasColumnType("character varying(20000)"); b.Property<Guid>("ResearchProjectId").HasColumnType("uuid"); b.Property<string>("Script").IsRequired().HasMaxLength(200000).HasColumnType("character varying(200000)"); b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)"); b.Property<string>("TagsJson").IsRequired().HasMaxLength(20000).HasColumnType("character varying(20000)"); b.Property<string>("ThumbnailConceptsJson").IsRequired().HasMaxLength(20000).HasColumnType("character varying(20000)"); b.Property<string>("TitleCandidatesJson").IsRequired().HasMaxLength(20000).HasColumnType("character varying(20000)"); b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<Guid>("WorkspaceId").HasColumnType("uuid"); b.HasKey("Id").HasName("pk_content_drafts"); b.HasIndex("ResearchProjectId").IsUnique().HasDatabaseName("ix_content_drafts_project_id"); b.HasIndex("WorkspaceId", "ResearchProjectId").HasDatabaseName("ix_content_drafts_workspace_id_project_id"); b.ToTable("content_drafts");
        });
        modelBuilder.Entity("YoutubeStudio.Api.Models.VideoProject", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid"); b.Property<Guid?>("ChannelId").HasColumnType("uuid"); b.Property<Guid?>("ContentDraftId").HasColumnType("uuid"); b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<string>("Prompt").IsRequired().HasMaxLength(10000).HasColumnType("character varying(10000)"); b.Property<string>("Script").HasMaxLength(100000).HasColumnType("character varying(100000)"); b.Property<string>("Source").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)"); b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)"); b.Property<string>("Title").HasMaxLength(500).HasColumnType("character varying(500)"); b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<Guid>("WorkspaceId").HasColumnType("uuid"); b.HasKey("Id").HasName("PK_video_projects"); b.HasIndex("ChannelId").HasDatabaseName("IX_video_projects_ChannelId"); b.HasIndex("ContentDraftId").HasDatabaseName("IX_video_projects_ContentDraftId"); b.HasIndex("WorkspaceId", "Status").HasDatabaseName("IX_video_projects_WorkspaceId_Status"); b.ToTable("video_projects");
        });
        modelBuilder.Entity("YoutubeStudio.Api.Models.ProductionJob", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid"); b.Property<int>("Attempt").HasColumnType("integer"); b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<string>("Error").HasMaxLength(4000).HasColumnType("character varying(4000)"); b.Property<string>("LastCompletedStage").HasMaxLength(50).HasColumnType("character varying(50)"); b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)"); b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<Guid>("VideoProjectId").HasColumnType("uuid"); b.HasKey("Id").HasName("PK_production_jobs"); b.HasIndex("VideoProjectId", "CreatedAtUtc").HasDatabaseName("IX_production_jobs_VideoProjectId_CreatedAtUtc"); b.ToTable("production_jobs");
        });
        modelBuilder.Entity("YoutubeStudio.Api.Models.ProductionArtifact", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid"); b.Property<string>("Content").HasMaxLength(200000).HasColumnType("character varying(200000)"); b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<string>("MetadataJson").HasMaxLength(200000).HasColumnType("character varying(200000)"); b.Property<string>("ProviderAssetId").IsRequired().HasMaxLength(500).HasColumnType("character varying(500)"); b.Property<string>("Type").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)"); b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<Guid>("VideoProjectId").HasColumnType("uuid"); b.HasKey("Id").HasName("PK_production_artifacts"); b.HasIndex("VideoProjectId", "Type").HasDatabaseName("IX_production_artifacts_VideoProjectId_Type"); b.ToTable("production_artifacts");
        });
        modelBuilder.Entity("YoutubeStudio.Api.Models.Channel", b => b.HasOne("YoutubeStudio.Api.Models.Workspace", "Workspace").WithMany("Channels").HasForeignKey("WorkspaceId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity("YoutubeStudio.Api.Models.Opportunity", b => b.HasOne("YoutubeStudio.Api.Models.Workspace", "Workspace").WithMany("Opportunities").HasForeignKey("WorkspaceId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity("YoutubeStudio.Api.Models.ResearchProject", b => b.HasOne("YoutubeStudio.Api.Models.Opportunity", "Opportunity").WithMany().HasForeignKey("OpportunityId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity("YoutubeStudio.Api.Models.ResearchBrief", b => b.HasOne("YoutubeStudio.Api.Models.ResearchProject", "ResearchProject").WithOne("Brief").HasForeignKey("YoutubeStudio.Api.Models.ResearchBrief", "ResearchProjectId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity("YoutubeStudio.Api.Models.ResearchSource", b => b.HasOne("YoutubeStudio.Api.Models.ResearchProject", "ResearchProject").WithMany("Sources").HasForeignKey("ResearchProjectId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity("YoutubeStudio.Api.Models.ResearchEvidence", b => b.HasOne("YoutubeStudio.Api.Models.ResearchSource", "ResearchSource").WithMany("Evidence").HasForeignKey("ResearchSourceId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity("YoutubeStudio.Api.Models.ResearchClaim", b => b.HasOne("YoutubeStudio.Api.Models.ResearchProject", "ResearchProject").WithMany().HasForeignKey("ResearchProjectId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity("YoutubeStudio.Api.Models.ResearchClaimEvidence", b => b.HasOne("YoutubeStudio.Api.Models.ResearchClaim", "ResearchClaim").WithMany("EvidenceLinks").HasForeignKey("ResearchClaimId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity("YoutubeStudio.Api.Models.ResearchClaimEvidence", b => b.HasOne("YoutubeStudio.Api.Models.ResearchEvidence", "ResearchEvidence").WithMany("ClaimLinks").HasForeignKey("ResearchEvidenceId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity("YoutubeStudio.Api.Models.FactCheckReport", b => b.HasOne("YoutubeStudio.Api.Models.ResearchProject", "ResearchProject").WithMany().HasForeignKey("ResearchProjectId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity("YoutubeStudio.Api.Models.FactCheckFinding", b => { b.HasOne("YoutubeStudio.Api.Models.FactCheckReport", "FactCheckReport").WithMany("Findings").HasForeignKey("FactCheckReportId").OnDelete(DeleteBehavior.Cascade).IsRequired(); b.HasOne("YoutubeStudio.Api.Models.ResearchClaim", "ResearchClaim").WithMany().HasForeignKey("ResearchClaimId").OnDelete(DeleteBehavior.Cascade).IsRequired(); });
        modelBuilder.Entity("YoutubeStudio.Api.Models.ContentDraft", b => b.HasOne("YoutubeStudio.Api.Models.ResearchProject", "ResearchProject").WithMany().HasForeignKey("ResearchProjectId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity("YoutubeStudio.Api.Models.VideoProject", b => { b.HasOne("YoutubeStudio.Api.Models.Channel", "Channel").WithMany().HasForeignKey("ChannelId").OnDelete(DeleteBehavior.SetNull); b.HasOne("YoutubeStudio.Api.Models.Workspace", "Workspace").WithMany().HasForeignKey("WorkspaceId").OnDelete(DeleteBehavior.Cascade).IsRequired(); });
        modelBuilder.Entity("YoutubeStudio.Api.Models.ProductionJob", b => b.HasOne("YoutubeStudio.Api.Models.VideoProject", "VideoProject").WithMany().HasForeignKey("VideoProjectId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity("YoutubeStudio.Api.Models.ProductionArtifact", b => b.HasOne("YoutubeStudio.Api.Models.VideoProject", "VideoProject").WithMany().HasForeignKey("VideoProjectId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity("YoutubeStudio.Api.Models.Channel", b => b.Navigation("Workspace"));
        modelBuilder.Entity("YoutubeStudio.Api.Models.Opportunity", b => b.Navigation("Workspace"));
        modelBuilder.Entity("YoutubeStudio.Api.Models.ResearchProject", b => { b.Navigation("Opportunity"); b.Navigation("Sources"); b.Navigation("Brief"); });
        modelBuilder.Entity("YoutubeStudio.Api.Models.ResearchBrief", b => b.Navigation("ResearchProject"));
        modelBuilder.Entity("YoutubeStudio.Api.Models.ResearchSource", b => b.Navigation("ResearchProject"));
        modelBuilder.Entity("YoutubeStudio.Api.Models.ResearchSource", b => b.Navigation("Evidence"));
        modelBuilder.Entity("YoutubeStudio.Api.Models.ResearchEvidence", b => { b.Navigation("ResearchSource"); b.Navigation("ClaimLinks"); });
        modelBuilder.Entity("YoutubeStudio.Api.Models.ResearchClaim", b => { b.Navigation("ResearchProject"); b.Navigation("EvidenceLinks"); });
        modelBuilder.Entity("YoutubeStudio.Api.Models.ResearchClaimEvidence", b => { b.Navigation("ResearchClaim"); b.Navigation("ResearchEvidence"); });
        modelBuilder.Entity("YoutubeStudio.Api.Models.FactCheckReport", b => { b.Navigation("ResearchProject"); b.Navigation("Findings"); });
        modelBuilder.Entity("YoutubeStudio.Api.Models.FactCheckFinding", b => { b.Navigation("FactCheckReport"); b.Navigation("ResearchClaim"); });
        modelBuilder.Entity("YoutubeStudio.Api.Models.ContentDraft", b => b.Navigation("ResearchProject"));
        modelBuilder.Entity("YoutubeStudio.Api.Models.VideoProject", b => { b.Navigation("Channel"); b.Navigation("Workspace"); });
        modelBuilder.Entity("YoutubeStudio.Api.Models.ProductionJob", b => b.Navigation("VideoProject"));
        modelBuilder.Entity("YoutubeStudio.Api.Models.ProductionArtifact", b => b.Navigation("VideoProject"));
        modelBuilder.Entity("YoutubeStudio.Api.Models.Workspace", b => { b.Navigation("Channels"); b.Navigation("Opportunities"); });
#pragma warning restore 612, 618
    }
}
