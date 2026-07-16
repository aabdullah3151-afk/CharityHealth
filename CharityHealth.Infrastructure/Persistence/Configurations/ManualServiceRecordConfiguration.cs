using CharityHealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharityHealth.Infrastructure.Persistence.Configurations;

public sealed class ManualServiceRecordConfiguration
    : IEntityTypeConfiguration<ManualServiceRecord>
{
    public void Configure(EntityTypeBuilder<ManualServiceRecord> builder)
    {
        builder.ToTable("ManualServiceRecords", "charity");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProviderUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.CreatedByUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(1500);

        builder.Property(x => x.Quantity)
            .HasDefaultValue(1);

        builder.HasIndex(x => new
        {
            x.ProviderUserId,
            x.ServiceType,
            x.ServiceDate
        });

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.ProviderUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Doctor>()
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
