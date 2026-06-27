using CharityHealth.Domain.Common;

namespace CharityHealth.Domain.Entities;

public class Specialty : BaseEntity
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Doctor> Doctors { get; set; } = [];
    public ICollection<MedicalRequest> MedicalRequests { get; set; } = [];
}
