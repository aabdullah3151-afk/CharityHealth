using CharityHealth.Domain.Common;

namespace CharityHealth.Domain.Entities;

public class Doctor : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public Guid SpecialtyId { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public string? ClinicAddress { get; set; }
    public string? ClinicPhone { get; set; }
    public string? WorkingDays { get; set; }

    public TimeOnly? WorkStartTime { get; set; }

    public TimeOnly? WorkEndTime { get; set; }

    /// <summary>Max free consultations allowed per day (default 1)</summary>
    public int MaxDailySlots { get; set; } = 1;

    public bool IsAvailable { get; set; } = true;
    public string? Notes { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Specialty Specialty { get; set; } = null!;
    public ICollection<Consultation> Consultations { get; set; } = [];
}
