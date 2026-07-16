using CharityHealth.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace CharityHealth.Domain.Entities;

/// <summary>
/// Core user entity — extends ASP.NET Core Identity.
/// All user types (Beneficiary, Doctor, Staff, Admin) share this table.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FullNameAr { get; set; } = string.Empty;
    public string FullNameEn { get; set; } = string.Empty;
    public UserType UserType { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string PreferredLanguage { get; set; } = "ar";

    // بيانات الجهة الطبية المتعاونة (صيدلية / معمل / مركز أشعة)
    public string? ContactPersonName { get; set; }
    public string? LicenseNumber { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public string? AddressAr { get; set; }
    public string? WorkingHours { get; set; }
    public string? WorkingDays { get; set; }
    public string? DescriptionAr { get; set; }
    public int DailyRequestCapacity { get; set; } = 20;
    public decimal DiscountPercentage { get; set; } = 0m;

    // Navigation
    public Beneficiary? Beneficiary { get; set; }
    public Doctor? Doctor { get; set; }
    public ICollection<OtpRecord> OtpRecords { get; set; } = [];
    public ICollection<LoginHistory> LoginHistories { get; set; } = [];
}
