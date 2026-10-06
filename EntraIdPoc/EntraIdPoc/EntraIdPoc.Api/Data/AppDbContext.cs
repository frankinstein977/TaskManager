using Microsoft.EntityFrameworkCore;
using EntraIdPoc.Api.Models;

namespace EntraIdPoc.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EntraOid).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.AppRole);
            entity.HasIndex(e => e.IsActive);

            entity.Property(e => e.EntraOid).HasMaxLength(36).IsRequired();
            entity.Property(e => e.EntraTid).HasMaxLength(36);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.DisplayName).HasMaxLength(255);
            entity.Property(e => e.GivenName).HasMaxLength(100);
            entity.Property(e => e.Surname).HasMaxLength(100);
            entity.Property(e => e.AppRole).HasMaxLength(50).HasDefaultValue("user");
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.Preferences).HasDefaultValue("{}");

            // Seed an admin user (will be linked on first login)
            // Note: In production, use a proper admin setup script
        });

        // Project entity configuration
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OwnerId);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.Title).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("active");

            entity.HasOne(e => e.Owner)
                .WithMany(u => u.Projects)
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(45); // IPv6 length

            entity.HasOne(e => e.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
