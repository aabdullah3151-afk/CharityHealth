using CharityHealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharityHealth.Infrastructure.Persistence.Configurations;

public class BeneficiaryConfiguration : IEntityTypeConfiguration<Beneficiary>
{
    public void Configure(EntityTypeBuilder<Beneficiary> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.NationalId).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.NationalId).IsUnique();
        b.Property(x => x.AddressAr).HasMaxLength(500);
        b.Property(x => x.City).HasMaxLength(100);

        b.HasOne(x => x.User)
            .WithOne(u => u.Beneficiary)
            .HasForeignKey<Beneficiary>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.LicenseNumber).HasMaxLength(50);
        b.HasIndex(x => x.LicenseNumber).IsUnique();
        b.Property(x => x.ClinicAddress).HasMaxLength(500);
        b.Property(x => x.ClinicPhone).HasMaxLength(20);

        b.HasOne(x => x.User)
            .WithOne(u => u.Doctor)
            .HasForeignKey<Doctor>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Specialty)
            .WithMany(s => s.Doctors)
            .HasForeignKey(x => x.SpecialtyId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class MedicalRequestConfiguration : IEntityTypeConfiguration<MedicalRequest>
{
    public void Configure(EntityTypeBuilder<MedicalRequest> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).IsRequired();
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.BeneficiaryId);
        b.Property(x => x.ReviewNoteAr).HasMaxLength(1000);
        b.Property(x => x.DescriptionAr).HasMaxLength(2000);
        b.Property(x => x.ProviderNoteAr).HasMaxLength(2000);
        b.Property(x => x.AssignedProviderUserId).HasMaxLength(450);
        b.HasIndex(x => x.ServiceType);
        b.HasIndex(x => x.AssignedProviderUserId);

        b.HasOne(x => x.Beneficiary)
            .WithMany(b => b.MedicalRequests)
            .HasForeignKey(x => x.BeneficiaryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Specialty)
            .WithMany(s => s.MedicalRequests)
            .HasForeignKey(x => x.SpecialtyId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.AssignedProvider)
            .WithMany()
            .HasForeignKey(x => x.AssignedProviderUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class QRCodeTokenConfiguration : IEntityTypeConfiguration<QRCodeToken>
{
    public void Configure(EntityTypeBuilder<QRCodeToken> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.TokenHash).HasMaxLength(256).IsRequired();
        b.HasIndex(x => x.TokenHash).IsUnique();  // Fast lookup on scan
        b.HasIndex(x => new { x.IsUsed, x.ExpiresAt });

        b.HasOne(x => x.Request)
            .WithOne(r => r.QRCodeToken)
            .HasForeignKey<QRCodeToken>(x => x.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ConsultationConfiguration : IEntityTypeConfiguration<Consultation>
{
    public void Configure(EntityTypeBuilder<Consultation> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.DiagnosisAr).HasMaxLength(2000).IsRequired();
        b.Property(x => x.DiagnosisEn).HasMaxLength(2000);
        b.Property(x => x.NotesAr).HasMaxLength(4000);
        b.Property(x => x.RecommendationsAr).HasMaxLength(2000);

        b.HasOne(x => x.QRCodeToken)
            .WithOne(q => q.Consultation)
            .HasForeignKey<Consultation>(x => x.QRCodeTokenId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Request)
            .WithOne(r => r.Consultation)
            .HasForeignKey<Consultation>(x => x.RequestId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class OtpRecordConfiguration : IEntityTypeConfiguration<OtpRecord>
{
    public void Configure(EntityTypeBuilder<OtpRecord> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
        b.Property(x => x.CodeHash).HasMaxLength(256).IsRequired();
        b.HasIndex(x => new { x.PhoneNumber, x.IsUsed });

        b.HasOne(x => x.User)
            .WithMany(u => u.OtpRecords)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Action).HasMaxLength(100).IsRequired();
        b.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        b.Property(x => x.IpAddress).HasMaxLength(50);
        b.HasIndex(x => x.Timestamp);
        b.HasIndex(x => x.UserId);
        // No query filter — audit logs are never deleted
    }
}

public class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.NameAr).IsUnique();
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
