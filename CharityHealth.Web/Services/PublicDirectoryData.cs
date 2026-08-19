using CharityHealth.Domain.Entities;
using CharityHealth.Domain.Enums;
using CharityHealth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CharityHealth.Web.Services;

public sealed record PublicDoctorCard(
    Guid Id,
    string NameAr,
    string SpecialtyName,
    string? LicenseNumber,
    string? ClinicAddress,
    string? ClinicPhone,
    string? WorkingDays,
    TimeOnly? WorkStartTime,
    TimeOnly? WorkEndTime,
    string? Notes,
    int CompletedServices,
    bool IsAvailable);

public sealed record PublicProviderCard(
    string UserId,
    ServiceRequestType ServiceType,
    string NameAr,
    string? PhoneNumber,
    string? ContactPersonName,
    string? LicenseNumber,
    string? Governorate,
    string? City,
    string? AddressAr,
    string? WorkingHours,
    string? WorkingDays,
    string? DescriptionAr,
    decimal DiscountPercentage,
    int DailyRequestCapacity,
    int CompletedServices);

public static class PublicDirectoryData
{
    public static async Task<List<PublicDoctorCard>> GetDoctorsAsync(
        AppDbContext db,
        int? take = null,
        CancellationToken ct = default)
    {
        var electronicCounts = await db.MedicalRequests
            .AsNoTracking()
            .Where(r =>
                r.ServiceType == ServiceRequestType.MedicalConsultation &&
                r.Status == RequestStatus.Completed &&
                r.DoctorId != null)
            .GroupBy(r => r.DoctorId!.Value)
            .Select(g => new { DoctorId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DoctorId, x => x.Count, ct);

        var manualCounts = await db.ManualServiceRecords
            .AsNoTracking()
            .Where(r =>
                !r.IsDeleted &&
                r.ServiceType == ServiceRequestType.MedicalConsultation &&
                r.DoctorId != null)
            .GroupBy(r => r.DoctorId!.Value)
            .Select(g => new { DoctorId = g.Key, Count = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.DoctorId, x => x.Count, ct);

        var rows = await db.Doctors
            .AsNoTracking()
            .Where(d => d.IsAvailable && !d.IsDeleted && d.User.IsActive)
            .Select(d => new PublicDoctorCard(
                d.Id,
                d.User.FullNameAr,
                d.Specialty.NameAr,
                d.LicenseNumber,
                d.ClinicAddress,
                d.ClinicPhone,
                d.WorkingDays,
                d.WorkStartTime,
                d.WorkEndTime,
                d.Notes,
                0,
                d.IsAvailable))
            .ToListAsync(ct);

        var ranked = rows
            .Select(d => d with
            {
                CompletedServices =
                    electronicCounts.GetValueOrDefault(d.Id) +
                    manualCounts.GetValueOrDefault(d.Id)
            })
            .OrderByDescending(d => d.CompletedServices)
            .ThenBy(d => d.NameAr);

        return (take is > 0 ? ranked.Take(take.Value) : ranked).ToList();
    }

    public static async Task<PublicDoctorCard?> GetDoctorAsync(
        AppDbContext db,
        Guid doctorId,
        CancellationToken ct = default)
    {
        var doctors = await GetDoctorsAsync(db, null, ct);
        return doctors.FirstOrDefault(d => d.Id == doctorId);
    }

    public static async Task<List<PublicProviderCard>> GetProvidersAsync(
        AppDbContext db,
        ServiceRequestType serviceType,
        int? take = null,
        CancellationToken ct = default)
    {
        if (serviceType == ServiceRequestType.MedicalConsultation)
            return [];

        var roleNames = serviceType switch
        {
            ServiceRequestType.PharmacyMedication => new[] { "Pharmacy", "Pharmacist" },
            ServiceRequestType.LaboratoryTest => new[] { "Laboratory" },
            ServiceRequestType.RadiologyScan => new[] { "RadiologyCenter" },
            _ => Array.Empty<string>()
        };

        var providerIds = await (
            from user in db.Users.AsNoTracking()
            join userRole in db.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
            join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where user.IsActive && role.Name != null && roleNames.Contains(role.Name)
            select user.Id
        ).Distinct().ToListAsync(ct);

        if (providerIds.Count == 0)
            return [];

        var electronicCounts = await db.MedicalRequests
            .AsNoTracking()
            .Where(r =>
                r.ServiceType == serviceType &&
                r.Status == RequestStatus.Completed &&
                r.AssignedProviderUserId != null &&
                providerIds.Contains(r.AssignedProviderUserId))
            .GroupBy(r => r.AssignedProviderUserId!)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);

        var manualCounts = await db.ManualServiceRecords
            .AsNoTracking()
            .Where(r =>
                !r.IsDeleted &&
                r.ServiceType == serviceType &&
                providerIds.Contains(r.ProviderUserId))
            .GroupBy(r => r.ProviderUserId)
            .Select(g => new { UserId = g.Key, Count = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);

        var rows = await db.Users
            .AsNoTracking()
            .Where(u => providerIds.Contains(u.Id) && u.IsActive)
            .Select(u => new PublicProviderCard(
                u.Id,
                serviceType,
                u.FullNameAr,
                u.PhoneNumber,
                u.ContactPersonName,
                u.LicenseNumber,
                u.Governorate,
                u.City,
                u.AddressAr,
                u.WorkingHours,
                u.WorkingDays,
                u.DescriptionAr,
                u.DiscountPercentage,
                u.DailyRequestCapacity,
                0))
            .ToListAsync(ct);

        var ranked = rows
            .Select(p => p with
            {
                CompletedServices =
                    electronicCounts.GetValueOrDefault(p.UserId) +
                    manualCounts.GetValueOrDefault(p.UserId)
            })
            .OrderByDescending(p => p.CompletedServices)
            .ThenBy(p => p.NameAr);

        return (take is > 0 ? ranked.Take(take.Value) : ranked).ToList();
    }

    public static async Task<PublicProviderCard?> GetProviderAsync(
        AppDbContext db,
        ServiceRequestType serviceType,
        string userId,
        CancellationToken ct = default)
    {
        var providers = await GetProvidersAsync(db, serviceType, null, ct);
        return providers.FirstOrDefault(p => p.UserId == userId);
    }

    public static ServiceRequestType? ServiceTypeFromSlug(string? slug) =>
        slug?.Trim().ToLowerInvariant() switch
        {
            "pharmacies" or "pharmacy" => ServiceRequestType.PharmacyMedication,
            "laboratories" or "laboratory" or "labs" => ServiceRequestType.LaboratoryTest,
            "radiology" or "radiology-centers" => ServiceRequestType.RadiologyScan,
            _ => null
        };

    public static string SlugFor(ServiceRequestType type) => type switch
    {
        ServiceRequestType.PharmacyMedication => "pharmacies",
        ServiceRequestType.LaboratoryTest => "laboratories",
        ServiceRequestType.RadiologyScan => "radiology-centers",
        _ => "doctors"
    };

    public static string ProviderTitle(ServiceRequestType type) => type switch
    {
        ServiceRequestType.PharmacyMedication => "الصيدليات",
        ServiceRequestType.LaboratoryTest => "معامل التحاليل",
        ServiceRequestType.RadiologyScan => "مراكز الأشعة",
        _ => "مقدمو الخدمة"
    };

    public static string ProviderIcon(ServiceRequestType type) => type switch
    {
        ServiceRequestType.PharmacyMedication => "💊",
        ServiceRequestType.LaboratoryTest => "🧪",
        ServiceRequestType.RadiologyScan => "🩻",
        _ => "🏥"
    };
}
