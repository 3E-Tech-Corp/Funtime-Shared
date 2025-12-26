using Microsoft.EntityFrameworkCore;
using Funtime.Identity.Api.Models;

namespace Funtime.Identity.Api.Data;

/// <summary>
/// Database context for the notification system (separate fxEmail database)
/// </summary>
public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<MailProfile> MailProfiles { get; set; }
    public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
    public DbSet<NotificationTask> NotificationTasks { get; set; }
    public DbSet<NotificationOutbox> NotificationOutbox { get; set; }
    public DbSet<NotificationHistory> NotificationHistory { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // MailProfile configuration
        modelBuilder.Entity<MailProfile>(entity =>
        {
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.SiteKey);
            entity.HasIndex(e => e.IsActive);
        });

        // NotificationTemplate configuration
        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasIndex(e => e.Code);
            entity.HasIndex(e => new { e.Code, e.SiteKey, e.Language }).IsUnique();
            entity.HasIndex(e => e.SiteKey);
            entity.HasIndex(e => e.Type);
        });

        // NotificationTask configuration
        modelBuilder.Entity<NotificationTask>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.SiteKey);
            entity.HasIndex(e => e.Status);
            entity.HasOne(e => e.MailProfile)
                .WithMany()
                .HasForeignKey(e => e.MailProfileId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Template)
                .WithMany()
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // NotificationOutbox configuration
        modelBuilder.Entity<NotificationOutbox>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.ScheduledAt);
            entity.HasIndex(e => e.NextRetryAt);
            entity.HasIndex(e => e.SiteKey);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasOne(e => e.Task)
                .WithMany()
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // NotificationHistory configuration
        modelBuilder.Entity<NotificationHistory>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.SiteKey);
            entity.HasIndex(e => e.SentAt);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
