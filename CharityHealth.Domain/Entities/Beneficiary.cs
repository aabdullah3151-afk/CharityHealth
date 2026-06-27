using CharityHealth.Domain.Common;
using CharityHealth.Domain.Enums;

namespace CharityHealth.Domain.Entities;

public class Beneficiary : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string? AddressAr { get; set; }
    public string? AddressEn { get; set; }
    public string? City { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public ICollection<MedicalRequest> MedicalRequests { get; set; } = [];
}
