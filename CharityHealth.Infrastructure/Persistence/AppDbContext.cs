using CharityHealth.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CharityHealth.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    // ── Domain DbSets ──────────────────────────────────
    public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<MedicalRequest> MedicalRequests => Set<MedicalRequest>();
    public DbSet<RequestDocument> RequestDocuments => Set<RequestDocument>();
    public DbSet<QRCodeToken> QRCodeTokens => Set<QRCodeToken>();
    public DbSet<Consultation> Consultations => Set<Consultation>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<OtpRecord> OtpRecords => Set<OtpRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all IEntityTypeConfiguration classes in this assembly
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Rename Identity tables to cleaner names
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.ContactPersonName).HasMaxLength(200);
            entity.Property(x => x.LicenseNumber).HasMaxLength(100);
            entity.Property(x => x.Governorate).HasMaxLength(150);
            entity.Property(x => x.City).HasMaxLength(150);
            entity.Property(x => x.AddressAr).HasMaxLength(500);
            entity.Property(x => x.WorkingHours).HasMaxLength(250);
            entity.Property(x => x.WorkingDays).HasMaxLength(100);
            entity.Property(x => x.DescriptionAr).HasMaxLength(1500);
            entity.Property(x => x.DailyRequestCapacity).HasDefaultValue(20);
            entity.Property(x => x.DiscountPercentage).HasPrecision(5, 2).HasDefaultValue(0m);
        });
        builder.HasDefaultSchema("charity");
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        SetAuditableFields();
        return await base.SaveChangesAsync(ct);
    }

    private void SetAuditableFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Domain.Common.BaseEntity entity)
            {
                if (entry.State == EntityState.Added)
                    entity.CreatedAt = DateTime.UtcNow;
                else
                    entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
