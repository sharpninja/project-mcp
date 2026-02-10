using Microsoft.EntityFrameworkCore;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMCP.TodoEngine.Data;

public sealed class TodoEngineDbContext : DbContext
{
    public TodoEngineDbContext(DbContextOptions<TodoEngineDbContext> options)
        : base(options)
    {
    }

    public DbSet<Enterprise> Enterprises => Set<Enterprise>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectResource> ProjectResources => Set<ProjectResource>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<Release> Releases => Set<Release>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Requirement> Requirements => Set<Requirement>();
    public DbSet<Standard> Standards => Set<Standard>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<Keyword> Keywords => Set<Keyword>();
    public DbSet<Domain> Domains => Set<Domain>();
    public DbSet<SystemEntity> Systems => Set<SystemEntity>();
    public DbSet<Asset> Assets => Set<Asset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Enterprise>(entity =>
        {
            entity.ToTable("enterprises");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DisplayId).IsRequired();
            entity.Property(e => e.Name).IsRequired();
            entity.HasIndex(e => e.DisplayId).IsUnique();
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.DisplayId).IsRequired();
            entity.Property(p => p.Name).IsRequired();
            entity.HasIndex(p => new { p.EnterpriseId, p.DisplayId }).IsUnique();
            entity.HasOne(p => p.Enterprise)
                .WithMany()
                .HasForeignKey(p => p.EnterpriseId);
        });

        modelBuilder.Entity<ProjectResource>(entity =>
        {
            entity.ToTable("project_resources");
            entity.HasKey(pr => new { pr.ProjectId, pr.ResourceId });
            entity.HasOne(pr => pr.Project)
                .WithMany()
                .HasForeignKey(pr => pr.ProjectId);
            entity.HasOne(pr => pr.Resource)
                .WithMany()
                .HasForeignKey(pr => pr.ResourceId);
        });

        modelBuilder.Entity<WorkItem>(entity =>
        {
            entity.ToTable("work_items");
            entity.HasKey(w => w.Id);
            entity.Property(w => w.DisplayId).IsRequired();
            entity.Property(w => w.Title).IsRequired();
            entity.HasIndex(w => new { w.ProjectId, w.DisplayId }).IsUnique();
            entity.HasOne(w => w.Project)
                .WithMany()
                .HasForeignKey(w => w.ProjectId);
            entity.HasOne(w => w.Parent)
                .WithMany()
                .HasForeignKey(w => w.ParentId);
        });

        modelBuilder.Entity<Milestone>(entity =>
        {
            entity.ToTable("milestones");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.DisplayId).IsRequired();
            entity.Property(m => m.Title).IsRequired();
            entity.HasIndex(m => new { m.EnterpriseId, m.DisplayId }).IsUnique();
            entity.HasOne(m => m.Enterprise)
                .WithMany()
                .HasForeignKey(m => m.EnterpriseId);
        });

        modelBuilder.Entity<Release>(entity =>
        {
            entity.ToTable("releases");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.DisplayId).IsRequired();
            entity.Property(r => r.Name).IsRequired();
            entity.HasIndex(r => new { r.ProjectId, r.DisplayId }).IsUnique();
            entity.HasOne(r => r.Project)
                .WithMany()
                .HasForeignKey(r => r.ProjectId);
        });

        modelBuilder.Entity<Resource>(entity =>
        {
            entity.ToTable("resources");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.DisplayId).IsRequired();
            entity.Property(r => r.Name).IsRequired();
            entity.Property(r => r.OAuth2Sub).HasColumnName("oauth2_sub"); // override convention: OAuth2Sub -> oauth2_sub not o_auth2sub
            entity.HasIndex(r => new { r.EnterpriseId, r.DisplayId }).IsUnique();
            entity.HasOne(r => r.Enterprise)
                .WithMany()
                .HasForeignKey(r => r.EnterpriseId);
        });

        modelBuilder.Entity<Requirement>(entity =>
        {
            entity.ToTable("requirements");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.DisplayId).IsRequired();
            entity.Property(r => r.Title).IsRequired();
            entity.HasIndex(r => new { r.ProjectId, r.DisplayId }).IsUnique();
            entity.HasOne(r => r.Project)
                .WithMany()
                .HasForeignKey(r => r.ProjectId);
            entity.HasOne(r => r.Keyword)
                .WithMany()
                .HasForeignKey(r => r.KeywordId)
                .IsRequired(false);
        });

        modelBuilder.Entity<Standard>(entity =>
        {
            entity.ToTable("standards");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.DisplayId).IsRequired();
            entity.Property(s => s.Title).IsRequired();
            entity.HasIndex(s => new { s.EnterpriseId, s.DisplayId }).IsUnique();
            entity.HasOne(s => s.Enterprise)
                .WithMany()
                .HasForeignKey(s => s.EnterpriseId);
        });

        modelBuilder.Entity<Issue>(entity =>
        {
            entity.ToTable("issues");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.DisplayId).IsRequired();
            entity.Property(i => i.Title).IsRequired();
            entity.HasIndex(i => new { i.ProjectId, i.DisplayId }).IsUnique();
            entity.HasOne(i => i.Project)
                .WithMany()
                .HasForeignKey(i => i.ProjectId);
        });

        modelBuilder.Entity<Keyword>(entity =>
        {
            entity.ToTable("keywords");
            entity.HasKey(k => k.Id);
            entity.Property(k => k.DisplayId).IsRequired();
            entity.Property(k => k.Name).IsRequired();
            entity.HasIndex(k => new { k.EnterpriseId, k.DisplayId }).IsUnique();
            entity.HasOne(k => k.Enterprise)
                .WithMany()
                .HasForeignKey(k => k.EnterpriseId);
        });

        modelBuilder.Entity<Domain>(entity =>
        {
            entity.ToTable("domains");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.DisplayId).IsRequired();
            entity.Property(d => d.Name).IsRequired();
            entity.HasIndex(d => new { d.EnterpriseId, d.DisplayId }).IsUnique();
            entity.HasOne(d => d.Enterprise)
                .WithMany()
                .HasForeignKey(d => d.EnterpriseId);
        });

        modelBuilder.Entity<SystemEntity>(entity =>
        {
            entity.ToTable("systems");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.DisplayId).IsRequired();
            entity.Property(s => s.Name).IsRequired();
            entity.HasIndex(s => new { s.EnterpriseId, s.DisplayId }).IsUnique();
            entity.HasOne(s => s.Enterprise)
                .WithMany()
                .HasForeignKey(s => s.EnterpriseId);
        });

        modelBuilder.Entity<Asset>(entity =>
        {
            entity.ToTable("assets");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.DisplayId).IsRequired();
            entity.Property(a => a.Name).IsRequired();
            entity.HasIndex(a => new { a.EnterpriseId, a.DisplayId }).IsUnique();
            entity.HasOne(a => a.Enterprise)
                .WithMany()
                .HasForeignKey(a => a.EnterpriseId);
        });
    }
}
