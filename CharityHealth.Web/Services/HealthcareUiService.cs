using System.Security.Cryptography;
using System.Text;
using CharityHealth.Domain.Entities;
using CharityHealth.Domain.Enums;
using CharityHealth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CharityHealth.Web.Services;

public record UiActionResult(bool Succeeded, string Message)
{
    public static UiActionResult Ok(string message) => new(true, message);
    public static UiActionResult Fail(string message) => new(false, message);
}

public record UiCreateResult(bool Succeeded, string Message, Guid? EntityId)
{
    public static UiCreateResult Ok(Guid id, string message) => new(true, message, id);
    public static UiCreateResult Fail(string message) => new(false, message, null);
}

public record DashboardStatsDto(
    int TotalBeneficiaries,
    int TotalDoctors,
    int TotalRequests,
    int PendingRequests,
    int ApprovedRequests,
    int RejectedRequests,
    int CompletedRequests,
    int SpecialtiesCount
);
 
public record DoctorCapacityStatusDto(
    Guid DoctorId,
    string DoctorName,
    int DailyCapacity,
    int UsedToday,
    int RemainingToday,
    bool IsFull,
    string WorkingDays
);
 
public record RequestWorkItem(
    Guid Id,
    string BeneficiaryName,
    string BeneficiaryPhone,
    string SpecialtyName,
    string City,
    RequestStatus Status,
    string StatusAr,
    string StatusCss,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    string? DescriptionAr,
    string? ReviewNoteAr,
    int DocumentsCount,
    bool HasQrCode,
    Guid? DoctorId = null,
    string? DoctorName = null,
    DateOnly? AppointmentDate = null,
    int? DoctorMaxDailySlots = null,
    ServiceRequestType ServiceType = ServiceRequestType.MedicalConsultation,
    string ServiceTypeAr = "استشارة طبية",
    string? AssignedProviderUserId = null,
    string? ProviderName = null,
    string? ProviderNoteAr = null,
    DateTime? FulfilledAt = null
);

public record PartnerAccountOption(
    string UserId,
    string NameAr,
    string Email,
    string PhoneNumber,
    ServiceRequestType ServiceType,
    string? Governorate,
    string? City,
    string? AddressAr,
    string? WorkingHours,
    string? WorkingDays,
    string? DescriptionAr,
    decimal DiscountPercentage,
    int DailyCapacity,
    int TodayRequests,
    int RemainingToday,
    bool IsFull
);

public record PartnerAvailableDay(
    DateOnly Date,
    string DayNameAr,
    int BookedRequests,
    int DailyCapacity,
    int RemainingRequests,
    bool IsFull
);

public record PartnerDashboardDto(
    string NameAr,
    string Email,
    string PhoneNumber,
    string? ContactPersonName,
    string? LicenseNumber,
    string? Governorate,
    string? City,
    string? AddressAr,
    string? WorkingHours,
    string? WorkingDays,
    string? DescriptionAr,
    decimal DiscountPercentage,
    int DailyCapacity,
    int TodayRequests,
    int RemainingToday,
    int PendingApprovedRequests,
    int CompletedRequests
);

public record PartnerRequestDocument(
    Guid Id,
    string FileName,
    string FilePath,
    long FileSizeBytes,
    DocumentType DocumentType
);


public record PartnerQrScanDto(
    Guid QrTokenId,
    Guid RequestId,
    string BeneficiaryName,
    string BeneficiaryPhone,
    string City,
    string ServiceTypeAr,
    string ProviderName,
    DateTime SubmittedAt,
    DateOnly? AppointmentDate,
    DateTime? ReviewedAt,
    DateTime ExpiresAt,
    string? DescriptionAr,
    int DocumentsCount,
    IReadOnlyList<PartnerRequestDocument> Documents
);

public record PartnerQrLookupResult(
    bool Succeeded,
    string Message,
    PartnerQrScanDto? Data
)
{
    public static PartnerQrLookupResult Ok(PartnerQrScanDto data) =>
        new(true, "تم التحقق من QR بنجاح.", data);

    public static PartnerQrLookupResult Fail(string message) =>
        new(false, message, null);
}

public record PartnerQrCompletionInput(
    string PrimaryResult,
    string? ReferenceNumber,
    DateTime? ExpectedDeliveryAt,
    string? AdditionalNotes
);


public record DoctorCaseDetailsDto(
    Guid Id,
    string BeneficiaryName,
    string BeneficiaryPhone,
    string City,
    string SpecialtyName,
    RequestStatus Status,
    string StatusAr,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    DateOnly? AppointmentDate,
    string? DescriptionAr,
    string? ReviewNoteAr,
    IReadOnlyList<PartnerRequestDocument> Documents,
    string? DiagnosisAr,
    string? RecommendationsAr,
    string? NotesAr,
    DateTime? ConsultedAt,
    bool HasQrCode
);

public record DoctorCaseInput(
    string DiagnosisAr,
    string? RecommendationsAr,
    string? NotesAr
);

public record DoctorListItem(
    Guid Id,
    string UserId,
    string FullNameAr,
    string PhoneNumber,
    string SpecialtyName,
    string? LicenseNumber,
    string? ClinicAddress,
    int MaxDailySlots,
    bool IsAvailable,
    string? WorkingDays,
    TimeOnly? WorkStartTime,
    TimeOnly? WorkEndTime
);

public record SpecialtyListItem(
    Guid Id,
    string NameAr,
    string NameEn,
    bool IsActive,
    int DoctorsCount,
    int RequestsCount,
    string? DescriptionAr = null,
    string? DescriptionEn = null
);

public record AuditLogListItem(
    Guid Id,
    DateTimeOffset Timestamp,
    string? UserId,
    string Action,
    string EntityType,
    string? EntityId,
    string? IpAddress
);

public record MonthlyDashboardPoint(
    string Month,
    int Requests,
    int Approved,
    int Completed
);

public record SpecialtyDashboardPoint(
    string Name,
    int Count,
    int Percent,
    string ColorHex
);

public record NotificationListItem(
    Guid Id,
    NotificationType Type,
    string TitleAr,
    string BodyAr,
    string? Url,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt,
    string Icon,
    string IconBackground,
    string IconColor
);

public sealed class HealthcareUiService(AppDbContext db)
{
    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var totalBeneficiaries = await db.Beneficiaries.CountAsync(ct);
        var totalDoctors = await db.Doctors.CountAsync(ct);
        var totalRequests = await db.MedicalRequests.CountAsync(ct);

        var pending = await db.MedicalRequests.CountAsync(
            r => r.Status == RequestStatus.Submitted || r.Status == RequestStatus.UnderReview,
            ct
        );

        var approved = await db.MedicalRequests.CountAsync(r => r.Status == RequestStatus.Approved, ct);
        var rejected = await db.MedicalRequests.CountAsync(r => r.Status == RequestStatus.Rejected, ct);
        var completed = await db.MedicalRequests.CountAsync(r => r.Status == RequestStatus.Completed, ct);
        var specialties = await db.Specialties.CountAsync(s => s.IsActive, ct);

        return new DashboardStatsDto(
            totalBeneficiaries,
            totalDoctors,
            totalRequests,
            pending,
            approved,
            rejected,
            completed,
            specialties
        );
    }

    public async Task<List<RequestWorkItem>> GetRequestsAsync(RequestStatus? status = null, CancellationToken ct = default)
    {
        var query = db.MedicalRequests
            .Include(r => r.Specialty)
            .Include(r => r.Doctor)
                .ThenInclude(d => d!.User)
            .Include(r => r.AssignedProvider)
            .Include(r => r.Documents)
            .Include(r => r.QRCodeToken)
            .Include(r => r.Beneficiary)
                .ThenInclude(b => b.User)
            .AsNoTracking();

        if (status is not null)
        {
            query = query.Where(r => r.Status == status);
        }

        return await query
            .OrderByDescending(r => r.SubmittedAt)
            .Select(r => new RequestWorkItem(
                r.Id,
                r.Beneficiary.User.FullNameAr,
                r.Beneficiary.User.PhoneNumber ?? "—",
                r.Specialty == null ? ServiceTypeAr(r.ServiceType) : r.Specialty.NameAr,
                r.Beneficiary.City ?? "—",
                r.Status,
                StatusAr(r.Status),
                StatusCss(r.Status),
                r.SubmittedAt,
                r.ReviewedAt,
                r.DescriptionAr,
                r.ReviewNoteAr,
                r.Documents.Count,
                r.QRCodeToken != null,
                r.DoctorId,
                r.Doctor == null ? null : r.Doctor.User.FullNameAr,
                r.AppointmentDate,
                r.Doctor == null ? null : r.Doctor.MaxDailySlots,
                r.ServiceType,
                ServiceTypeAr(r.ServiceType),
                r.AssignedProviderUserId,
                r.AssignedProvider == null ? null : r.AssignedProvider.FullNameAr,
                r.ProviderNoteAr,
                r.FulfilledAt
            ))
            .ToListAsync(ct);
    }

    public async Task<List<RequestWorkItem>> GetBeneficiaryRequestsAsync(string userId, CancellationToken ct = default)
    {
        var beneficiary = await db.Beneficiaries
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.UserId == userId, ct);

        if (beneficiary is null)
        {
            return [];
        }

        return await db.MedicalRequests
            .Where(r => r.BeneficiaryId == beneficiary.Id)
            .Include(r => r.Specialty)
            .Include(r => r.Doctor)
                .ThenInclude(d => d!.User)
            .Include(r => r.AssignedProvider)
            .Include(r => r.Documents)
            .Include(r => r.QRCodeToken)
            .Include(r => r.Beneficiary)
                .ThenInclude(b => b.User)
            .AsNoTracking()
            .OrderByDescending(r => r.SubmittedAt)
            .Select(r => new RequestWorkItem(
                r.Id,
                r.Beneficiary.User.FullNameAr,
                r.Beneficiary.User.PhoneNumber ?? "—",
                r.Specialty == null ? ServiceTypeAr(r.ServiceType) : r.Specialty.NameAr,
                r.Beneficiary.City ?? "—",
                r.Status,
                StatusAr(r.Status),
                StatusCss(r.Status),
                r.SubmittedAt,
                r.ReviewedAt,
                r.DescriptionAr,
                r.ReviewNoteAr,
                r.Documents.Count,
                r.QRCodeToken != null,
                r.DoctorId,
                r.Doctor == null ? null : r.Doctor.User.FullNameAr,
                r.AppointmentDate,
                r.Doctor == null ? null : r.Doctor.MaxDailySlots,
                r.ServiceType,
                ServiceTypeAr(r.ServiceType),
                r.AssignedProviderUserId,
                r.AssignedProvider == null ? null : r.AssignedProvider.FullNameAr,
                r.ProviderNoteAr,
                r.FulfilledAt
            ))
            .ToListAsync(ct);
    }

    public async Task<List<RequestWorkItem>> GetDoctorQueueAsync(string doctorUserId, CancellationToken ct = default)
    {
        var doctor = await db.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == doctorUserId, ct);

        if (doctor is null)
        {
            return [];
        }

        return await db.MedicalRequests
            .Where(r =>
                r.DoctorId == doctor.Id &&
                r.Status != RequestStatus.Draft &&
                r.Status != RequestStatus.Rejected
            )
            .Include(r => r.Specialty)
            .Include(r => r.Doctor)
                .ThenInclude(d => d!.User)
            .Include(r => r.AssignedProvider)
            .Include(r => r.Documents)
            .Include(r => r.QRCodeToken)
            .Include(r => r.Beneficiary)
                .ThenInclude(b => b.User)
            .AsNoTracking()
            .OrderByDescending(r => r.Status == RequestStatus.Approved)
            .ThenBy(r => r.SubmittedAt)
            .Select(r => new RequestWorkItem(
                r.Id,
                r.Beneficiary.User.FullNameAr,
                r.Beneficiary.User.PhoneNumber ?? "—",
                r.Specialty == null ? ServiceTypeAr(r.ServiceType) : r.Specialty.NameAr,
                r.Beneficiary.City ?? "—",
                r.Status,
                StatusAr(r.Status),
                StatusCss(r.Status),
                r.SubmittedAt,
                r.ReviewedAt,
                r.DescriptionAr,
                r.ReviewNoteAr,
                r.Documents.Count,
                r.QRCodeToken != null,
                r.DoctorId,
                r.Doctor == null ? null : r.Doctor.User.FullNameAr,
                r.AppointmentDate,
                r.Doctor == null ? null : r.Doctor.MaxDailySlots,
                r.ServiceType,
                ServiceTypeAr(r.ServiceType),
                r.AssignedProviderUserId,
                r.AssignedProvider == null ? null : r.AssignedProvider.FullNameAr,
                r.ProviderNoteAr,
                r.FulfilledAt
            ))
            .ToListAsync(ct);
    }

    public async Task<List<PartnerAccountOption>> GetActivePartnerAccountsAsync(
        ServiceRequestType serviceType,
        CancellationToken ct = default)
    {
        if (serviceType == ServiceRequestType.MedicalConsultation)
        {
            return [];
        }

        var roles = ExpectedProviderRoles(serviceType);
        var providerIds = await (
            from user in db.Users.AsNoTracking()
            join userRole in db.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
            join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where user.IsActive && role.Name != null && roles.Contains(role.Name)
            select user.Id
        ).Distinct().ToListAsync(ct);

        if (providerIds.Count == 0)
        {
            return [];
        }

        var todayOnly = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayCounts = await db.MedicalRequests.AsNoTracking()
            .Where(r => r.AssignedProviderUserId != null
                        && providerIds.Contains(r.AssignedProviderUserId)
                        && r.ServiceType == serviceType
                        && r.AppointmentDate == todayOnly
                        && r.Status != RequestStatus.Draft
                        && r.Status != RequestStatus.Rejected)
            .GroupBy(r => r.AssignedProviderUserId!)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);

        var users = await db.Users.AsNoTracking()
            .Where(u => providerIds.Contains(u.Id) && u.IsActive)
            .OrderBy(u => u.FullNameAr)
            .ToListAsync(ct);

        return users.Select(user =>
        {
            var capacity = user.DailyRequestCapacity <= 0 ? 20 : user.DailyRequestCapacity;
            var used = todayCounts.GetValueOrDefault(user.Id);
            var remaining = Math.Max(0, capacity - used);
            return new PartnerAccountOption(
                user.Id,
                user.FullNameAr,
                user.Email ?? "—",
                user.PhoneNumber ?? "—",
                serviceType,
                user.Governorate,
                user.City,
                user.AddressAr,
                user.WorkingHours,
                user.WorkingDays,
                user.DescriptionAr,
                NormalizeDiscount(user.DiscountPercentage),
                capacity,
                used,
                remaining,
                false);
        }).ToList();
    }

    public async Task<List<PartnerAvailableDay>> GetPartnerAvailableDaysAsync(
        string providerUserId,
        ServiceRequestType serviceType,
        int daysAhead = 30,
        CancellationToken ct = default)
    {
        if (serviceType == ServiceRequestType.MedicalConsultation) return [];
        var provider = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == providerUserId && u.IsActive, ct);
        if (provider is null) return [];

        var capacity = provider.DailyRequestCapacity <= 0 ? 20 : provider.DailyRequestCapacity;
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var end = start.AddDays(Math.Clamp(daysAhead, 7, 60));
        var counts = await db.MedicalRequests.AsNoTracking()
            .Where(r => r.AssignedProviderUserId == providerUserId
                        && r.ServiceType == serviceType
                        && r.AppointmentDate != null
                        && r.AppointmentDate.Value >= start
                        && r.AppointmentDate.Value <= end
                        && r.Status != RequestStatus.Draft
                        && r.Status != RequestStatus.Rejected)
            .GroupBy(r => r.AppointmentDate!.Value)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Date, x => x.Count, ct);

        var result = new List<PartnerAvailableDay>();
        for (var date = start; date <= end && result.Count < 14; date = date.AddDays(1))
        {
            if (!IsWorkingOnDate(provider.WorkingDays, date)) continue;
            var booked = counts.GetValueOrDefault(date);
            var remaining = Math.Max(0, capacity - booked);
            result.Add(new PartnerAvailableDay(date, DayNameAr(date.DayOfWeek), booked, capacity, remaining, remaining <= 0));
        }

        return result;
    }

    public async Task<int> GetPartnerCompletedServicesAsync(
        string providerUserId,
        ServiceRequestType serviceType,
        CancellationToken ct = default)
    {
        var electronicCompleted = await db.MedicalRequests
            .AsNoTracking()
            .CountAsync(r =>
                r.AssignedProviderUserId == providerUserId
                && r.ServiceType == serviceType
                && r.Status == RequestStatus.Completed, ct);

        var manualCompleted = await db.ManualServiceRecords
            .AsNoTracking()
            .Where(r =>
                !r.IsDeleted
                && r.ProviderUserId == providerUserId
                && r.ServiceType == serviceType)
            .SumAsync(r => (int?)r.Quantity, ct) ?? 0;

        return electronicCompleted + manualCompleted;
    }


    public async Task<int> GetDoctorCompletedServicesAsync(
        string doctorUserId,
        CancellationToken ct = default)
    {
        var doctorId = await db.Doctors
            .AsNoTracking()
            .Where(d =>
                d.UserId == doctorUserId
                && !d.IsDeleted)
            .Select(d => (Guid?)d.Id)
            .FirstOrDefaultAsync(ct);

        if (doctorId is null)
            return 0;

        var electronicCompleted = await db.MedicalRequests
            .AsNoTracking()
            .CountAsync(r =>
                r.DoctorId == doctorId.Value
                && r.ServiceType ==
                    ServiceRequestType.MedicalConsultation
                && r.Status == RequestStatus.Completed,
                ct);

        var manualCompleted = await db.ManualServiceRecords
            .AsNoTracking()
            .Where(r =>
                !r.IsDeleted
                && r.DoctorId == doctorId.Value
                && r.ServiceType ==
                    ServiceRequestType.MedicalConsultation)
            .SumAsync(r => (int?)r.Quantity, ct) ?? 0;

        return electronicCompleted + manualCompleted;
    }


    public async Task<PartnerDashboardDto?> GetPartnerDashboardAsync(
        string providerUserId,
        ServiceRequestType serviceType,
        CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == providerUserId, ct);
        if (user is null) return null;

        var todayOnly = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayRequests = await db.MedicalRequests.AsNoTracking().CountAsync(r =>
            r.AssignedProviderUserId == providerUserId
            && r.ServiceType == serviceType
            && r.AppointmentDate == todayOnly
            && r.Status != RequestStatus.Draft && r.Status != RequestStatus.Rejected, ct);

        var pendingApproved = await db.MedicalRequests.AsNoTracking().CountAsync(r =>
            r.AssignedProviderUserId == providerUserId
            && r.ServiceType == serviceType
            && r.Status == RequestStatus.Approved, ct);

        var completed =
            await GetPartnerCompletedServicesAsync(
                providerUserId,
                serviceType,
                ct);

        var capacity = user.DailyRequestCapacity <= 0 ? 20 : user.DailyRequestCapacity;
        return new PartnerDashboardDto(
            user.FullNameAr,
            user.Email ?? "—",
            user.PhoneNumber ?? "—",
            user.ContactPersonName,
            user.LicenseNumber ?? "—",
            user.Governorate,
            user.City,
            user.AddressAr,
            user.WorkingHours,
            user.WorkingDays,
            user.DescriptionAr,
            NormalizeDiscount(user.DiscountPercentage),
            capacity,
            todayRequests,
            Math.Max(0, capacity - todayRequests),
            pendingApproved,
            completed);
    }

    public async Task<UiCreateResult> SubmitPartnerServiceRequestAsync(
        string beneficiaryUserId,
        ServiceRequestType serviceType,
        string providerUserId,
        string? descriptionAr,
        DateOnly? appointmentDate = null,
        CancellationToken ct = default)
    {
        if (serviceType == ServiceRequestType.MedicalConsultation)
        {
            return UiCreateResult.Fail("استخدم نموذج الكشف الطبي لطلب الاستشارة.");
        }

        if (string.IsNullOrWhiteSpace(descriptionAr))
        {
            return UiCreateResult.Fail("يجب كتابة تفاصيل الطلب.");
        }

        if (descriptionAr.Trim().Length > 2000)
        {
            return UiCreateResult.Fail("تفاصيل الطلب لا تتجاوز 2000 حرف.");
        }

        var beneficiary = await db.Beneficiaries
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.UserId == beneficiaryUserId, ct);

        if (beneficiary is null)
        {
            return UiCreateResult.Fail("لم يتم العثور على بيانات المستفيد.");
        }

        var expectedRoles = ExpectedProviderRoles(serviceType);
        var providerIsValid = await (
            from user in db.Users
            join userRole in db.UserRoles on user.Id equals userRole.UserId
            join role in db.Roles on userRole.RoleId equals role.Id
            where user.Id == providerUserId
                  && user.IsActive
                  && role.Name != null
                  && expectedRoles.Contains(role.Name)
            select user.Id
        ).AnyAsync(ct);

        if (!providerIsValid)
        {
            return UiCreateResult.Fail("الجهة المختارة غير متاحة حالياً.");
        }

        var provider = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == providerUserId, ct);
        if (provider is null)
        {
            return UiCreateResult.Fail("تعذر قراءة بيانات الجهة المختارة.");
        }

        var minDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var capacity = provider.DailyRequestCapacity <= 0 ? 20 : provider.DailyRequestCapacity;
        if (appointmentDate is null) return UiCreateResult.Fail(AppointmentRequiredMessage(serviceType));
        if (appointmentDate < minDate) return UiCreateResult.Fail("لا يمكن اختيار يوم سابق.");
        if (!IsWorkingOnDate(provider.WorkingDays, appointmentDate.Value))
            return UiCreateResult.Fail($"{ProviderNameAr(serviceType)} لا يعمل في اليوم المختار.");

        var bookedRequests = await db.MedicalRequests.CountAsync(r =>
            r.AssignedProviderUserId == providerUserId
            && r.ServiceType == serviceType
            && r.AppointmentDate == appointmentDate
            && r.Status != RequestStatus.Draft && r.Status != RequestStatus.Rejected, ct);

        if (bookedRequests >= capacity)
        {
            return UiCreateResult.Fail($"اكتمل عدد طلبات {ProviderNameAr(serviceType)} في اليوم المختار. اختر يومًا آخر.");
        }

        var hasActiveRequest = await db.MedicalRequests.AnyAsync(r =>
            r.BeneficiaryId == beneficiary.Id
            && r.ServiceType == serviceType
            && (r.Status == RequestStatus.Submitted
                || r.Status == RequestStatus.UnderReview
                || r.Status == RequestStatus.Approved), ct);

        if (hasActiveRequest)
        {
            return UiCreateResult.Fail($"لديك {ServiceTypeAr(serviceType)} قيد المتابعة بالفعل. انتظر إتمامه أولاً.");
        }

        var request = new MedicalRequest
        {
            BeneficiaryId = beneficiary.Id,
            SpecialtyId = null,
            DoctorId = null,
            AppointmentDate = appointmentDate,
            ServiceType = serviceType,
            AssignedProviderUserId = providerUserId,
            DescriptionAr = descriptionAr.Trim(),
            Status = RequestStatus.Submitted,
            SubmittedAt = DateTime.UtcNow
        };

        db.MedicalRequests.Add(request);

        AddAuditLog(
            beneficiaryUserId,
            "ServiceRequest.Submitted",
            "MedicalRequest",
            request.Id.ToString(),
            null,
            $"ServiceType={serviceType}; ProviderUserId={providerUserId}; BeneficiaryId={beneficiary.Id}; AppointmentDate={appointmentDate}");

        await db.SaveChangesAsync(ct);
        return UiCreateResult.Ok(request.Id, "تم تقديم الطلب بنجاح وهو الآن قيد مراجعة الإدارة.");
    }

    public async Task CancelNewPartnerServiceRequestAsync(
        Guid requestId,
        string beneficiaryUserId,
        CancellationToken ct = default)
    {
        var request = await db.MedicalRequests
            .Include(r => r.Beneficiary)
            .FirstOrDefaultAsync(r => r.Id == requestId
                                      && r.Beneficiary.UserId == beneficiaryUserId
                                      && r.Status == RequestStatus.Submitted
                                      && r.ServiceType != ServiceRequestType.MedicalConsultation, ct);

        if (request is null) return;

        db.MedicalRequests.Remove(request);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<RequestWorkItem>> GetPartnerRequestsAsync(
        string providerUserId,
        ServiceRequestType serviceType,
        CancellationToken ct = default)
    {
        var rows = await GetRequestsAsync(null, ct);

        return rows
            .Where(r => r.ServiceType == serviceType
                        && r.AssignedProviderUserId == providerUserId
                        && r.Status is RequestStatus.Approved or RequestStatus.Completed)
            .OrderByDescending(r => r.Status == RequestStatus.Approved)
            .ThenByDescending(r => r.ReviewedAt ?? r.SubmittedAt)
            .ToList();
    }

    public async Task<List<PartnerRequestDocument>> GetPartnerRequestDocumentsAsync(
        Guid requestId,
        string providerUserId,
        ServiceRequestType serviceType,
        CancellationToken ct = default)
    {
        return await db.RequestDocuments
            .AsNoTracking()
            .Where(d => d.RequestId == requestId
                        && d.Request.ServiceType == serviceType
                        && d.Request.AssignedProviderUserId == providerUserId
                        && (d.Request.Status == RequestStatus.Approved
                            || d.Request.Status == RequestStatus.Completed))
            .OrderBy(d => d.DocumentType)
            .ThenBy(d => d.FileName)
            .Select(d => new PartnerRequestDocument(
                d.Id,
                d.FileName,
                d.FilePath,
                d.FileSizeBytes,
                d.DocumentType))
            .ToListAsync(ct);
    }


    public async Task<PartnerQrLookupResult> GetPartnerRequestByQrAsync(
        Guid qrCodeTokenId,
        string providerUserId,
        ServiceRequestType serviceType,
        CancellationToken ct = default)
    {
        if (serviceType == ServiceRequestType.MedicalConsultation)
        {
            return PartnerQrLookupResult.Fail("استخدم شاشة الطبيب لمسح QR الخاص بالكشف الطبي.");
        }

        if (string.IsNullOrWhiteSpace(providerUserId))
        {
            return PartnerQrLookupResult.Fail("تعذر قراءة حساب الجهة الحالية. سجّل الدخول مرة أخرى.");
        }

        var providerIsActive = await IsActiveProviderForServiceAsync(providerUserId, serviceType, ct);
        if (!providerIsActive)
        {
            return PartnerQrLookupResult.Fail("حساب الجهة غير نشط أو لا يملك الصلاحية المطلوبة.");
        }

        var qr = await db.QRCodeTokens
            .AsNoTracking()
            .Include(q => q.Request)
                .ThenInclude(r => r.Beneficiary)
                    .ThenInclude(b => b.User)
            .Include(q => q.Request)
                .ThenInclude(r => r.AssignedProvider)
            .Include(q => q.Request)
                .ThenInclude(r => r.Documents)
            .FirstOrDefaultAsync(q => q.Id == qrCodeTokenId && !q.IsDeleted, ct);

        if (qr is null)
        {
            return PartnerQrLookupResult.Fail("رمز QR غير موجود أو غير صالح.");
        }

        var request = qr.Request;

        if (request.ServiceType != serviceType)
        {
            return PartnerQrLookupResult.Fail($"هذا QR خاص بـ {ServiceTypeAr(request.ServiceType)} وليس بالخدمة الحالية.");
        }

        if (!string.Equals(request.AssignedProviderUserId, providerUserId, StringComparison.Ordinal))
        {
            return PartnerQrLookupResult.Fail("هذا الطلب مخصص لجهة أخرى ولا يمكن لحسابك تنفيذه.");
        }

        if (request.Status == RequestStatus.Completed || qr.IsUsed)
        {
            return PartnerQrLookupResult.Fail("تم استخدام QR وإنهاء هذا الطلب من قبل.");
        }

        if (request.Status != RequestStatus.Approved)
        {
            return PartnerQrLookupResult.Fail("لا يمكن تنفيذ الطلب قبل موافقة الإدارة.");
        }

        if (qr.ExpiresAt <= DateTime.UtcNow)
        {
            return PartnerQrLookupResult.Fail("انتهت صلاحية QR. اطلب من الإدارة إعادة اعتماد الطلب.");
        }

        var documents = request.Documents
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.DocumentType)
            .ThenBy(d => d.FileName)
            .Select(d => new PartnerRequestDocument(
                d.Id,
                d.FileName,
                d.FilePath,
                d.FileSizeBytes,
                d.DocumentType))
            .ToList();

        var data = new PartnerQrScanDto(
            qr.Id,
            request.Id,
            request.Beneficiary.User.FullNameAr,
            request.Beneficiary.User.PhoneNumber ?? "—",
            request.Beneficiary.City ?? "—",
            ServiceTypeAr(request.ServiceType),
            request.AssignedProvider?.FullNameAr ?? "الجهة المختارة",
            request.SubmittedAt,
            request.AppointmentDate,
            request.ReviewedAt,
            qr.ExpiresAt,
            request.DescriptionAr,
            documents.Count,
            documents);

        return PartnerQrLookupResult.Ok(data);
    }

    public async Task<UiActionResult> CompletePartnerRequestByQrAsync(
        Guid qrCodeTokenId,
        string providerUserId,
        ServiceRequestType serviceType,
        PartnerQrCompletionInput input,
        CancellationToken ct = default)
    {
        if (serviceType == ServiceRequestType.MedicalConsultation)
        {
            return UiActionResult.Fail("استخدم شاشة الطبيب لإنهاء طلب الكشف.");
        }

        if (input is null || string.IsNullOrWhiteSpace(input.PrimaryResult))
        {
            return UiActionResult.Fail(PrimaryResultRequiredMessage(serviceType));
        }

        if (input.PrimaryResult.Trim().Length > 3000
            || (input.AdditionalNotes?.Trim().Length ?? 0) > 3000
            || (input.ReferenceNumber?.Trim().Length ?? 0) > 120)
        {
            return UiActionResult.Fail("البيانات المدخلة أطول من الحد المسموح.");
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var qr = await db.QRCodeTokens
            .Include(q => q.Request)
                .ThenInclude(r => r.Beneficiary)
            .Include(q => q.Request)
                .ThenInclude(r => r.AssignedProvider)
            .FirstOrDefaultAsync(q => q.Id == qrCodeTokenId && !q.IsDeleted, ct);

        if (qr is null)
        {
            return UiActionResult.Fail("رمز QR غير موجود أو غير صالح.");
        }

        var request = qr.Request;

        if (request.ServiceType != serviceType)
        {
            return UiActionResult.Fail($"هذا QR خاص بـ {ServiceTypeAr(request.ServiceType)} وليس بالخدمة الحالية.");
        }

        if (!string.Equals(request.AssignedProviderUserId, providerUserId, StringComparison.Ordinal))
        {
            return UiActionResult.Fail("هذا الطلب مخصص لجهة أخرى ولا يمكن لحسابك تنفيذه.");
        }

        if (!await IsActiveProviderForServiceAsync(providerUserId, serviceType, ct))
        {
            return UiActionResult.Fail("حساب الجهة غير نشط أو لا يملك الصلاحية المطلوبة.");
        }

        if (request.Status == RequestStatus.Completed || qr.IsUsed)
        {
            return UiActionResult.Fail("تم استخدام QR وإنهاء هذا الطلب من قبل.");
        }

        if (request.Status != RequestStatus.Approved)
        {
            return UiActionResult.Fail("لا يمكن تنفيذ الطلب قبل موافقة الإدارة.");
        }

        if (qr.ExpiresAt <= DateTime.UtcNow)
        {
            return UiActionResult.Fail("انتهت صلاحية QR. اطلب من الإدارة إعادة اعتماد الطلب.");
        }

        var finalNote = BuildPartnerCompletionNote(serviceType, input);

        request.Status = RequestStatus.Completed;
        request.ProviderNoteAr = finalNote;
        request.FulfilledAt = DateTime.UtcNow;

        qr.IsUsed = true;
        qr.UsedAt = DateTime.UtcNow;
        qr.UsedByDoctorId = providerUserId;

        AddNotification(
            request.Beneficiary.UserId,
            NotificationType.ServiceCompleted,
            CompletionTitle(serviceType),
            $"تم إنهاء {ServiceTypeAr(serviceType)} بواسطة {request.AssignedProvider?.FullNameAr ?? "الجهة المختارة"}. يمكنك مراجعة النتيجة من صفحة طلباتي.",
            $"/portal/request/{request.Id}",
            "MedicalRequest",
            request.Id.ToString());

        AddAuditLog(
            providerUserId,
            "ServiceRequest.CompletedByQr",
            "MedicalRequest",
            request.Id.ToString(),
            null,
            $"ServiceType={serviceType}; ProviderUserId={providerUserId}; QRCodeTokenId={qr.Id}; Note={finalNote}");

        AddAuditLog(
            providerUserId,
            "QR.Used",
            "QRCodeToken",
            qr.Id.ToString(),
            null,
            $"RequestId={request.Id}; ServiceType={serviceType}; UsedAt={qr.UsedAt:O}");

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return UiActionResult.Ok(PartnerCompletionSuccessMessage(serviceType));
    }


public async Task<UiActionResult> CompletePartnerRequestAsync(
    Guid requestId,
    string providerUserId,
    ServiceRequestType serviceType,
    string? providerNote,
    CancellationToken ct = default)
{
    if (serviceType == ServiceRequestType.MedicalConsultation)
    {
        return UiActionResult.Fail("استخدم شاشة الطبيب لإنهاء طلب الكشف.");
    }

    providerNote = providerNote?.Trim();

    if (string.IsNullOrWhiteSpace(providerNote))
    {
        return UiActionResult.Fail(PrimaryResultRequiredMessage(serviceType));
    }

    if (providerNote.Length > 3000)
    {
        return UiActionResult.Fail("النتيجة أو بيانات التنفيذ أطول من الحد المسموح.");
    }

    if (string.IsNullOrWhiteSpace(providerUserId)
        || !await IsActiveProviderForServiceAsync(providerUserId, serviceType, ct))
    {
        return UiActionResult.Fail("حساب الجهة غير نشط أو لا يملك الصلاحية المطلوبة.");
    }

    await using var tx = await db.Database.BeginTransactionAsync(ct);

    var request = await db.MedicalRequests
        .Include(r => r.QRCodeToken)
        .Include(r => r.Beneficiary)
        .Include(r => r.AssignedProvider)
        .FirstOrDefaultAsync(r => r.Id == requestId, ct);

    if (request is null)
    {
        return UiActionResult.Fail("الطلب غير موجود.");
    }

    if (request.ServiceType != serviceType)
    {
        return UiActionResult.Fail($"هذا الطلب خاص بـ {ServiceTypeAr(request.ServiceType)} وليس بالخدمة الحالية.");
    }

    if (!string.Equals(request.AssignedProviderUserId, providerUserId, StringComparison.Ordinal))
    {
        return UiActionResult.Fail("هذا الطلب مخصص لجهة أخرى ولا يمكن لحسابك فتحه أو تعديله.");
    }

    if (request.Status is not RequestStatus.Approved and not RequestStatus.Completed)
    {
        return UiActionResult.Fail("يمكن استكمال الطلب بعد موافقة الإدارة فقط.");
    }

    var wasCompleted = request.Status == RequestStatus.Completed;

    request.Status = RequestStatus.Completed;
    request.ProviderNoteAr = providerNote;
    request.FulfilledAt ??= DateTime.UtcNow;

    if (request.QRCodeToken is not null && !request.QRCodeToken.IsUsed)
    {
        request.QRCodeToken.IsUsed = true;
        request.QRCodeToken.UsedAt = DateTime.UtcNow;
        request.QRCodeToken.UsedByDoctorId = providerUserId;
    }

    AddNotification(
        request.Beneficiary.UserId,
        NotificationType.ServiceCompleted,
        wasCompleted ? $"تم تحديث نتيجة {ServiceTypeAr(serviceType)}" : CompletionTitle(serviceType),
        wasCompleted
            ? $"تم تحديث نتيجة {ServiceTypeAr(serviceType)} بواسطة {request.AssignedProvider?.FullNameAr ?? "الجهة المختارة"}."
            : $"تم إنهاء {ServiceTypeAr(serviceType)} بواسطة {request.AssignedProvider?.FullNameAr ?? "الجهة المختارة"}. يمكنك مراجعة النتيجة من صفحة طلباتي.",
        $"/portal/request/{request.Id}",
        "MedicalRequest",
        request.Id.ToString());

    AddAuditLog(
        providerUserId,
        wasCompleted ? "ServiceRequest.ResultUpdatedDirectly" : "ServiceRequest.CompletedDirectly",
        "MedicalRequest",
        request.Id.ToString(),
        null,
        $"ServiceType={serviceType}; ProviderUserId={providerUserId}; QRWasScanned=false; Note={providerNote}");

    await db.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);

    return UiActionResult.Ok(
        wasCompleted
            ? "تم تحديث بيانات التنفيذ بنجاح."
            : "تم تنفيذ الطلب مباشرة بنجاح. مسح QR ما زال متاحًا كطريقة اختيارية للطلبات الأخرى.");
}


public async Task<DoctorCaseDetailsDto?> GetDoctorCaseAsync(
    Guid requestId,
    string doctorUserId,
    CancellationToken ct = default)
{
    var doctor = await db.Doctors
        .AsNoTracking()
        .FirstOrDefaultAsync(d =>
            d.UserId == doctorUserId &&
            d.IsAvailable &&
            !d.IsDeleted,
            ct);

    if (doctor is null)
    {
        return null;
    }

    var request = await db.MedicalRequests
        .Include(r => r.Beneficiary)
            .ThenInclude(b => b.User)
        .Include(r => r.Specialty)
        .Include(r => r.Documents)
        .Include(r => r.QRCodeToken)
        .Include(r => r.Consultation)
        .AsNoTracking()
        .FirstOrDefaultAsync(r =>
            r.Id == requestId &&
            r.DoctorId == doctor.Id &&
            r.ServiceType == ServiceRequestType.MedicalConsultation &&
            r.Status != RequestStatus.Draft &&
            r.Status != RequestStatus.Rejected,
            ct);

    if (request is null)
    {
        return null;
    }

    var documents = request.Documents
        .Where(d => !d.IsDeleted)
        .OrderBy(d => d.DocumentType)
        .ThenBy(d => d.FileName)
        .Select(d => new PartnerRequestDocument(
            d.Id,
            d.FileName,
            d.FilePath,
            d.FileSizeBytes,
            d.DocumentType))
        .ToList();

    return new DoctorCaseDetailsDto(
        request.Id,
        request.Beneficiary.User.FullNameAr,
        request.Beneficiary.User.PhoneNumber ?? "—",
        request.Beneficiary.City ?? "—",
        request.Specialty?.NameAr ?? "استشارة طبية",
        request.Status,
        StatusAr(request.Status),
        request.SubmittedAt,
        request.ReviewedAt,
        request.AppointmentDate,
        request.DescriptionAr,
        request.ReviewNoteAr,
        documents,
        request.Consultation?.DiagnosisAr,
        request.Consultation?.RecommendationsAr,
        request.Consultation?.NotesAr,
        request.Consultation?.ConsultedAt,
        request.QRCodeToken is not null);
}

public async Task<UiActionResult> SaveDoctorCaseAsync(
    Guid requestId,
    string doctorUserId,
    DoctorCaseInput input,
    CancellationToken ct = default)
{
    if (input is null || string.IsNullOrWhiteSpace(input.DiagnosisAr))
    {
        return UiActionResult.Fail("التشخيص أو نتيجة المعاينة مطلوبة.");
    }

    var diagnosis = input.DiagnosisAr.Trim();
    var recommendations = string.IsNullOrWhiteSpace(input.RecommendationsAr)
        ? null
        : input.RecommendationsAr.Trim();
    var notes = string.IsNullOrWhiteSpace(input.NotesAr)
        ? null
        : input.NotesAr.Trim();

    if (diagnosis.Length > 3000
        || (recommendations?.Length ?? 0) > 3000
        || (notes?.Length ?? 0) > 3000)
    {
        return UiActionResult.Fail("إحدى الخانات أطول من الحد المسموح.");
    }

    var doctor = await db.Doctors
        .FirstOrDefaultAsync(d =>
            d.UserId == doctorUserId &&
            d.IsAvailable &&
            !d.IsDeleted,
            ct);

    if (doctor is null)
    {
        return UiActionResult.Fail("حساب الطبيب غير متاح أو غير نشط.");
    }

    await using var tx = await db.Database.BeginTransactionAsync(ct);

    var request = await db.MedicalRequests
        .Include(r => r.Beneficiary)
        .Include(r => r.QRCodeToken)
        .Include(r => r.Consultation)
        .FirstOrDefaultAsync(r =>
            r.Id == requestId &&
            r.DoctorId == doctor.Id &&
            r.ServiceType == ServiceRequestType.MedicalConsultation,
            ct);

    if (request is null)
    {
        return UiActionResult.Fail("الحالة غير موجودة أو غير مخصصة للطبيب الحالي.");
    }

    if (request.Status is not RequestStatus.Approved and not RequestStatus.Completed)
    {
        return UiActionResult.Fail("يمكن تسجيل المعاينة بعد موافقة الإدارة فقط.");
    }

    if (request.QRCodeToken is null)
    {
        return UiActionResult.Fail("الطلب المعتمد لا يحتوي على رمز خدمة داخلي. أعد اعتماد الطلب من الإدارة ثم حاول مرة أخرى.");
    }

    var wasCompleted = request.Status == RequestStatus.Completed;
    var now = DateTime.UtcNow;

    request.Status = RequestStatus.Completed;
    request.FulfilledAt ??= now;

    if (!request.QRCodeToken.IsUsed)
    {
        request.QRCodeToken.IsUsed = true;
        request.QRCodeToken.UsedAt = now;
        request.QRCodeToken.UsedByDoctorId = doctorUserId;
    }

    if (request.Consultation is null)
    {
        db.Consultations.Add(new Consultation
        {
            RequestId = request.Id,
            QRCodeTokenId = request.QRCodeToken.Id,
            DoctorId = doctorUserId,
            DiagnosisAr = diagnosis,
            DiagnosisEn = string.Empty,
            RecommendationsAr = recommendations,
            RecommendationsEn = null,
            NotesAr = notes,
            NotesEn = null,
            ConsultedAt = now
        });
    }
    else
    {
        request.Consultation.DoctorId = doctorUserId;
        request.Consultation.DiagnosisAr = diagnosis;
        request.Consultation.RecommendationsAr = recommendations;
        request.Consultation.NotesAr = notes;
        request.Consultation.ConsultedAt = now;
    }

    const string resultMarker = "\n--- نتيجة الطبيب ---\n";
    var adminNote = request.ReviewNoteAr ?? string.Empty;
    var markerIndex = adminNote.IndexOf(resultMarker, StringComparison.Ordinal);
    if (markerIndex >= 0)
    {
        adminNote = adminNote[..markerIndex].TrimEnd();
    }

    var resultText = $"التشخيص: {diagnosis}";
    if (!string.IsNullOrWhiteSpace(recommendations))
    {
        resultText += $"\nالعلاج والتوصيات: {recommendations}";
    }
    if (!string.IsNullOrWhiteSpace(notes))
    {
        resultText += $"\nملاحظات الطبيب: {notes}";
    }

    request.ReviewNoteAr = string.IsNullOrWhiteSpace(adminNote)
        ? resultText
        : adminNote + resultMarker + resultText;

    AddNotification(
        request.Beneficiary.UserId,
        NotificationType.ConsultationCompleted,
        wasCompleted ? "تم تحديث نتيجة الكشف" : "تم تسجيل نتيجة الكشف",
        wasCompleted
            ? "قام الطبيب بتحديث بيانات المعاينة. يمكنك مراجعة النتيجة من صفحة طلباتي."
            : "تم تسجيل نتيجة الكشف بواسطة الطبيب. يمكنك مراجعة النتيجة من صفحة طلباتي.",
        $"/portal/request/{request.Id}",
        "MedicalRequest",
        request.Id.ToString());

    AddAuditLog(
        doctorUserId,
        wasCompleted ? "Consultation.UpdatedDirectly" : "Consultation.CompletedDirectly",
        "MedicalRequest",
        request.Id.ToString(),
        null,
        $"DoctorUserId={doctorUserId}; QRWasScanned=false; Diagnosis={diagnosis}");

    await db.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);

    return UiActionResult.Ok(
        wasCompleted
            ? "تم تحديث بيانات المعاينة بنجاح."
            : "تم حفظ المعاينة وإكمال الحالة بنجاح دون إلزام بمسح QR.");
}

    public async Task<List<DoctorListItem>> GetDoctorsAsync(CancellationToken ct = default)
    {
        return await db.Doctors
            .Include(d => d.User)
            .Include(d => d.Specialty)
            .AsNoTracking()
            .OrderBy(d => d.User.FullNameAr)
            .Select(d => new DoctorListItem(
                d.Id,
                d.UserId,
                d.User.FullNameAr,
                d.User.PhoneNumber ?? "—",
                d.Specialty.NameAr,
                d.LicenseNumber ?? "—",
                d.ClinicAddress,
                d.MaxDailySlots,
                d.IsAvailable,
                d.WorkingDays,
                d.WorkStartTime,
                d.WorkEndTime
            ))
            .ToListAsync(ct);
    }

    public async Task<List<SpecialtyListItem>> GetSpecialtiesAsync(CancellationToken ct = default)
    {
        return await db.Specialties
            .Include(s => s.Doctors)
            .Include(s => s.MedicalRequests)
            .AsNoTracking()
            .OrderBy(s => s.NameAr)
            .Select(s => new SpecialtyListItem(
                s.Id,
                s.NameAr,
                s.NameEn,
                s.IsActive,
                s.Doctors.Count,
                s.MedicalRequests.Count,
                s.DescriptionAr,
                s.DescriptionEn
            ))
            .ToListAsync(ct);
    }

    public async Task<UiActionResult> CreateSpecialtyAsync(
        string? nameAr,
        string? nameEn,
        string? descriptionAr = null,
        string? descriptionEn = null,
        bool isActive = true,
        CancellationToken ct = default)
    {
        nameAr = nameAr?.Trim() ?? string.Empty;
        nameEn = nameEn?.Trim() ?? string.Empty;
        descriptionAr = string.IsNullOrWhiteSpace(descriptionAr) ? null : descriptionAr.Trim();
        descriptionEn = string.IsNullOrWhiteSpace(descriptionEn) ? null : descriptionEn.Trim();

        if (string.IsNullOrWhiteSpace(nameAr))
            return UiActionResult.Fail("اسم التخصص بالعربي مطلوب.");

        if (string.IsNullOrWhiteSpace(nameEn))
            return UiActionResult.Fail("اسم التخصص بالإنجليزي مطلوب.");

        var exists = await db.Specialties.AnyAsync(s => s.NameAr == nameAr || s.NameEn == nameEn, ct);

        if (exists)
            return UiActionResult.Fail("هذا التخصص موجود بالفعل.");

        var specialty = new Specialty
        {
            NameAr = nameAr,
            NameEn = nameEn,
            DescriptionAr = descriptionAr,
            DescriptionEn = descriptionEn,
            IsActive = isActive
        };

        db.Specialties.Add(specialty);
        AddAuditLog(null, "Specialty.Created", "Specialty", specialty.Id.ToString(), null, $"NameAr={specialty.NameAr}; NameEn={specialty.NameEn}; IsActive={specialty.IsActive}");
        await db.SaveChangesAsync(ct);

        return UiActionResult.Ok("تم إضافة التخصص بنجاح.");
    }

    public async Task<UiActionResult> UpdateSpecialtyAsync(
        Guid id,
        string? nameAr,
        string? nameEn,
        string? descriptionAr = null,
        string? descriptionEn = null,
        bool isActive = true,
        CancellationToken ct = default)
    {
        nameAr = nameAr?.Trim() ?? string.Empty;
        nameEn = nameEn?.Trim() ?? string.Empty;
        descriptionAr = string.IsNullOrWhiteSpace(descriptionAr) ? null : descriptionAr.Trim();
        descriptionEn = string.IsNullOrWhiteSpace(descriptionEn) ? null : descriptionEn.Trim();

        if (id == Guid.Empty)
            return UiActionResult.Fail("لم يتم تحديد التخصص المطلوب تعديله.");

        if (string.IsNullOrWhiteSpace(nameAr))
            return UiActionResult.Fail("اسم التخصص بالعربي مطلوب.");

        if (string.IsNullOrWhiteSpace(nameEn))
            return UiActionResult.Fail("اسم التخصص بالإنجليزي مطلوب.");

        var specialty = await db.Specialties.FirstOrDefaultAsync(s => s.Id == id, ct);

        if (specialty is null)
            return UiActionResult.Fail("التخصص غير موجود أو تم حذفه.");

        var exists = await db.Specialties.AnyAsync(s =>
            s.Id != id && (s.NameAr == nameAr || s.NameEn == nameEn),
            ct
        );

        if (exists)
            return UiActionResult.Fail("يوجد تخصص آخر بنفس الاسم العربي أو الإنجليزي.");

        var oldSpecialtyValues = $"NameAr={specialty.NameAr}; NameEn={specialty.NameEn}; IsActive={specialty.IsActive}";

        specialty.NameAr = nameAr;
        specialty.NameEn = nameEn;
        specialty.DescriptionAr = descriptionAr;
        specialty.DescriptionEn = descriptionEn;
        specialty.IsActive = isActive;

        AddAuditLog(null, "Specialty.Updated", "Specialty", specialty.Id.ToString(), oldSpecialtyValues, $"NameAr={specialty.NameAr}; NameEn={specialty.NameEn}; IsActive={specialty.IsActive}");
        await db.SaveChangesAsync(ct);

        return UiActionResult.Ok("تم تعديل التخصص بنجاح.");
    }

    public async Task<UiActionResult> DeleteSpecialtyAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            return UiActionResult.Fail("لم يتم تحديد التخصص المطلوب حذفه.");

        var specialty = await db.Specialties
            .Include(s => s.Doctors)
            .Include(s => s.MedicalRequests)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (specialty is null)
            return UiActionResult.Fail("التخصص غير موجود أو تم حذفه بالفعل.");

        if (specialty.Doctors.Count > 0 || specialty.MedicalRequests.Count > 0)
            return UiActionResult.Fail("لا يمكن حذف هذا التخصص لأنه مرتبط بأطباء أو طلبات. يمكنك تعطيله من زر التعديل بدل الحذف.");

        AddAuditLog(null, "Specialty.Deleted", "Specialty", specialty.Id.ToString(), $"NameAr={specialty.NameAr}; NameEn={specialty.NameEn}", null);
        db.Specialties.Remove(specialty);
        await db.SaveChangesAsync(ct);

        return UiActionResult.Ok("تم حذف التخصص بنجاح.");
    }

    public async Task<List<AuditLogListItem>> GetAuditLogsAsync(CancellationToken ct = default)
    {
        return await db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.Timestamp)
            .Take(500)
            .Select(a => new AuditLogListItem(
                a.Id,
                a.Timestamp,
                a.UserId,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.IpAddress
            ))
            .ToListAsync(ct);
    }

    public async Task<List<MonthlyDashboardPoint>> GetMonthlyDashboardAsync(int months = 6, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var start = currentMonthStart.AddMonths(-(months - 1));

        var rows = await db.MedicalRequests
            .AsNoTracking()
            .Where(r => r.SubmittedAt >= start)
            .Select(r => new { r.SubmittedAt, r.Status })
            .ToListAsync(ct);

        var arabicMonths = new[]
        {
            "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
            "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"
        };

        return Enumerable.Range(0, months)
            .Select(i =>
            {
                var monthDate = start.AddMonths(i);

                var monthRows = rows
                    .Where(r => r.SubmittedAt.Year == monthDate.Year && r.SubmittedAt.Month == monthDate.Month)
                    .ToList();

                return new MonthlyDashboardPoint(
                    arabicMonths[monthDate.Month - 1],
                    monthRows.Count,
                    monthRows.Count(r => r.Status == RequestStatus.Approved),
                    monthRows.Count(r => r.Status == RequestStatus.Completed)
                );
            })
            .ToList();
    }

    public async Task<List<SpecialtyDashboardPoint>> GetSpecialtyDashboardAsync(CancellationToken ct = default)
    {
        var palette = new[]
        {
            "#ef4444", "#3b82f6", "#f97316", "#ec4899", "#22c55e",
            "#14b8a6", "#a855f7", "#0ea5e9", "#8b5cf6"
        };

        var rows = await db.MedicalRequests
            .Include(r => r.Specialty)
            .AsNoTracking()
            .Where(r => r.ServiceType == ServiceRequestType.MedicalConsultation && r.Specialty != null)
            .Select(r => r.Specialty!.NameAr)
            .ToListAsync(ct);

        if (rows.Count == 0)
        {
            var activeSpecialties = await db.Specialties
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.NameAr)
                .Take(9)
                .Select(s => s.NameAr)
                .ToListAsync(ct);

            return activeSpecialties
                .Select((name, index) => new SpecialtyDashboardPoint(name, 0, 0, palette[index % palette.Length]))
                .ToList();
        }

        var total = rows.Count;

        return rows
            .GroupBy(name => name)
            .OrderByDescending(group => group.Count())
            .Take(9)
            .Select((group, index) => new SpecialtyDashboardPoint(
                group.Key,
                group.Count(),
                (int)Math.Round((double)group.Count() / total * 100),
                palette[index % palette.Length]
            ))
            .ToList();
    }

    public async Task<List<NotificationListItem>> GetNotificationsAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return [];
        }

        return await db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationListItem(
                n.Id,
                n.Type,
                n.TitleAr,
                n.BodyAr,
                n.Url,
                n.IsRead,
                n.CreatedAt,
                n.ReadAt,
                NotificationIcon(n.Type),
                NotificationIconBackground(n.Type),
                NotificationIconColor(n.Type)
            ))
            .ToListAsync(ct);
    }

    public async Task<int> GetUnreadNotificationsCountAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return 0;
        }

        return await db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsDeleted, ct);
    }

    public async Task<UiActionResult> MarkNotificationReadAsync(Guid id, string userId, CancellationToken ct = default)
    {
        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && !n.IsDeleted, ct);

        if (notification is null)
        {
            return UiActionResult.Fail("الإشعار غير موجود.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return UiActionResult.Ok("تم تحديد الإشعار كمقروء.");
    }

    public async Task<UiActionResult> MarkAllNotificationsReadAsync(string userId, CancellationToken ct = default)
    {
        var notifications = await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
            .ToListAsync(ct);

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return UiActionResult.Ok("تم تحديد كل الإشعارات كمقروءة.");
    }

    public async Task<UiActionResult> MarkUnderReviewAsync(Guid requestId, string reviewerId, CancellationToken ct = default)
    {
        var request = await db.MedicalRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);
        if (request is null) return UiActionResult.Fail("الطلب غير موجود");

        if (request.Status is RequestStatus.Approved or RequestStatus.Rejected or RequestStatus.Completed)
            return UiActionResult.Fail("لا يمكن نقل هذا الطلب للمراجعة في حالته الحالية");

        var oldStatus = request.Status;

        request.Status = RequestStatus.UnderReview;
        request.ReviewedBy = reviewerId;
        request.ReviewedAt = DateTime.UtcNow;

        AddAuditLog(reviewerId, "Request.UnderReview", "MedicalRequest", request.Id.ToString(), $"Status={oldStatus}", $"Status={request.Status}");
        await db.SaveChangesAsync(ct);
        return UiActionResult.Ok("تم نقل الطلب إلى قيد المراجعة");
    }

    public async Task<UiActionResult> ApproveRequestAsync(Guid requestId, string reviewerId, string? note, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var request = await db.MedicalRequests
            .Include(r => r.QRCodeToken)
            .Include(r => r.Beneficiary)
                .ThenInclude(b => b.User)
            .Include(r => r.Specialty)
            .Include(r => r.Doctor)
                .ThenInclude(d => d!.User)
            .Include(r => r.AssignedProvider)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (request is null) return UiActionResult.Fail("الطلب غير موجود");
        if (request.Status == RequestStatus.Completed) return UiActionResult.Fail("الطلب مكتمل بالفعل");
        if (request.Status == RequestStatus.Rejected) return UiActionResult.Fail("لا يمكن الموافقة على طلب مرفوض إلا بعد إعادة تقديمه.");

        Doctor? doctor = null;

        if (request.ServiceType == ServiceRequestType.MedicalConsultation)
        {
            if (request.SpecialtyId is null || request.DoctorId is null || request.AppointmentDate is null)
            {
                return UiActionResult.Fail("طلب الكشف غير مكتمل: يجب اختيار التخصص والطبيب ويوم الكشف.");
            }

            doctor = await db.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == request.DoctorId.Value && d.IsAvailable && !d.IsDeleted, ct);

            if (doctor is null)
            {
                return UiActionResult.Fail("الطبيب المختار غير متاح أو تم حذفه.");
            }

            if (doctor.SpecialtyId != request.SpecialtyId.Value)
            {
                return UiActionResult.Fail("الطبيب المختار لا يتبع تخصص الطلب.");
            }

            if (!IsWorkingOnDate(doctor.WorkingDays, request.AppointmentDate.Value))
            {
                return UiActionResult.Fail("الطبيب غير متاح في اليوم المختار.");
            }

            var approvedCountBefore = await db.MedicalRequests.CountAsync(r =>
                r.Id != request.Id
                && r.ServiceType == ServiceRequestType.MedicalConsultation
                && r.DoctorId == request.DoctorId
                && r.AppointmentDate == request.AppointmentDate
                && r.Status == RequestStatus.Approved,
                ct);

            if (approvedCountBefore >= doctor.MaxDailySlots)
            {
                return UiActionResult.Fail("هذا اليوم مكتمل للطبيب المختار ولا يمكن قبول طلبات إضافية.");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.AssignedProviderUserId))
            {
                return UiActionResult.Fail("يجب أن يحتوي الطلب على جهة خدمة محددة.");
            }

            if (request.AppointmentDate is null)
                return UiActionResult.Fail(AppointmentApprovalRequiredMessage(request.ServiceType));
            if (request.AppointmentDate < DateOnly.FromDateTime(DateTime.UtcNow))
                return UiActionResult.Fail("اليوم المختار أصبح في الماضي.");

            var provider = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.AssignedProviderUserId, ct);
            if (provider is null || !IsWorkingOnDate(provider.WorkingDays, request.AppointmentDate.Value))
                return UiActionResult.Fail($"{ProviderNameAr(request.ServiceType)} لا يعمل في اليوم المختار.");

            var capacity = provider.DailyRequestCapacity <= 0 ? 20 : provider.DailyRequestCapacity;
            var booked = await db.MedicalRequests.CountAsync(r => r.Id != request.Id
                && r.AssignedProviderUserId == request.AssignedProviderUserId
                && r.ServiceType == request.ServiceType
                && r.AppointmentDate == request.AppointmentDate
                && r.Status == RequestStatus.Approved, ct);
            if (booked >= capacity)
                return UiActionResult.Fail($"اليوم المختار مكتمل لدى {ProviderNameAr(request.ServiceType)}. اطلب من المستفيد اختيار يوم آخر.");

            var expectedRoles = ExpectedProviderRoles(request.ServiceType);
            var providerIsValid = await (
                from user in db.Users
                join userRole in db.UserRoles on user.Id equals userRole.UserId
                join role in db.Roles on userRole.RoleId equals role.Id
                where user.Id == request.AssignedProviderUserId
                      && user.IsActive
                      && role.Name != null
                      && expectedRoles.Contains(role.Name)
                select user.Id
            ).AnyAsync(ct);

            if (!providerIsValid)
            {
                return UiActionResult.Fail("الجهة المختارة غير نشطة أو لا تتبع نوع الخدمة المطلوب.");
            }
        }

        request.Status = RequestStatus.Approved;
        request.ReviewedBy = reviewerId;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewNoteAr = string.IsNullOrWhiteSpace(note)
            ? DefaultApprovalNote(request.ServiceType)
            : note.Trim();

        var qrExpiresAt = GetQrExpiryUtc(request.AppointmentDate);

        var qrWasCreated = request.QRCodeToken is null;

        if (request.QRCodeToken is null)
        {
            var raw = $"REQ:{request.Id}:BEN:{request.BeneficiaryId}:EXP:{qrExpiresAt:O}:{Guid.NewGuid():N}";
            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

            db.QRCodeTokens.Add(new QRCodeToken
            {
                RequestId = request.Id,
                TokenHash = tokenHash,
                ExpiresAt = qrExpiresAt,
                IsUsed = false
            });
        }
        else
        {
            request.QRCodeToken.ExpiresAt = qrExpiresAt;
            request.QRCodeToken.IsUsed = false;
            request.QRCodeToken.UsedAt = null;
            request.QRCodeToken.UsedByDoctorId = null;
        }

        AddNotification(
            request.Beneficiary.UserId,
            NotificationType.RequestApproved,
            "تم قبول طلبك وتوليد QR",
            BuildApprovalNotificationBody(request, doctor),
            $"/portal/request-qr/{request.Id}",
            "MedicalRequest",
            request.Id.ToString()
        );

        var autoRejectedCount = 0;

        if (request.ServiceType == ServiceRequestType.MedicalConsultation
            && request.DoctorId is not null
            && request.AppointmentDate is not null
            && doctor is not null)
        {
            autoRejectedCount = await RejectOtherPendingRequestsIfDayIsFullAsync(
                request.Id,
                request.DoctorId.Value,
                request.AppointmentDate.Value,
                doctor,
                reviewerId,
                ct);
        }

        AddAuditLog(
            reviewerId,
            "Request.Approved",
            "MedicalRequest",
            request.Id.ToString(),
            null,
            $"Status={request.Status}; ServiceType={request.ServiceType}; BeneficiaryId={request.BeneficiaryId}; SpecialtyId={request.SpecialtyId}; DoctorId={request.DoctorId}; ProviderUserId={request.AssignedProviderUserId}; AppointmentDate={request.AppointmentDate}");

        AddAuditLog(
            reviewerId,
            qrWasCreated ? "QR.Generated" : "QR.Renewed",
            "QRCodeToken",
            request.Id.ToString(),
            null,
            $"RequestId={request.Id}; ExpiresAt={qrExpiresAt:O}");

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return autoRejectedCount > 0
            ? UiActionResult.Ok($"تمت الموافقة على الطلب وتوليد QR. تم إلغاء {autoRejectedCount} طلب آخر لأن يوم الطبيب اكتمل.")
            : UiActionResult.Ok("تمت الموافقة على الطلب وتوليد QR وإرسال إشعار للمستفيد");
    }

    public async Task<UiActionResult> RejectRequestAsync(Guid requestId, string reviewerId, string? note, CancellationToken ct = default)
    {
        var request = await db.MedicalRequests
            .Include(r => r.Beneficiary)
            .Include(r => r.Specialty)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (request is null) return UiActionResult.Fail("الطلب غير موجود");
        if (request.Status == RequestStatus.Completed) return UiActionResult.Fail("لا يمكن رفض طلب مكتمل");

        request.Status = RequestStatus.Rejected;
        request.ReviewedBy = reviewerId;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewNoteAr = string.IsNullOrWhiteSpace(note)
            ? "تم رفض الطلب بعد المراجعة."
            : note.Trim();

        AddNotification(
            request.Beneficiary.UserId,
            NotificationType.RequestRejected,
            "تم رفض طلبك",
            $"تم رفض {ServiceTypeAr(request.ServiceType)}. يمكنك مراجعة تفاصيل الطلب من صفحة طلباتي.",
            "/portal/requests",
            "MedicalRequest",
            request.Id.ToString()
        );

        AddAuditLog(
            reviewerId,
            "Request.Rejected",
            "MedicalRequest",
            request.Id.ToString(),
            null,
            $"Status={request.Status}; BeneficiaryId={request.BeneficiaryId}; SpecialtyId={request.SpecialtyId}; Note={request.ReviewNoteAr}");

        await db.SaveChangesAsync(ct);
        return UiActionResult.Ok("تم رفض الطلب وإرسال إشعار للمستفيد");
    }

    public async Task<UiActionResult> CompleteRequestAsync(Guid requestId, string doctorUserId, string? note, CancellationToken ct = default)
    {
        var request = await db.MedicalRequests
            .Include(r => r.QRCodeToken)
            .Include(r => r.Beneficiary)
            .Include(r => r.Consultation)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (request is null) return UiActionResult.Fail("الطلب غير موجود");
        if (request.ServiceType != ServiceRequestType.MedicalConsultation) return UiActionResult.Fail("هذا الطلب تابع لجهة خدمة وليس لطبيب.");
        if (request.Status != RequestStatus.Approved) return UiActionResult.Fail("لا يمكن إنهاء الطلب قبل الموافقة عليه");

        var qr = request.QRCodeToken;

        if (qr is not null)
        {
            if (qr.ExpiresAt < DateTime.UtcNow) return UiActionResult.Fail("QR منتهي الصلاحية. برجاء إعادة اعتماد الطلب لتوليد صلاحية جديدة.");

            if (qr.IsUsed && request.Consultation is not null)
            {
                return UiActionResult.Fail("تم استخدام QR من قبل.");
            }

            if (qr.IsUsed && request.Consultation is null)
            {
                qr.IsUsed = false;
                qr.UsedAt = null;
                qr.UsedByDoctorId = null;
            }
        }

        await CompleteApprovedRequestCoreAsync(request, qr, doctorUserId, note, ct);
        return UiActionResult.Ok("تم تسجيل انتهاء الكشف وتحديث حالة الطلب");
    }

    public async Task<UiActionResult> CompleteRequestByQrAsync(Guid qrCodeTokenId, string doctorUserId, string? note, CancellationToken ct = default)
    {
        var qr = await db.QRCodeTokens
            .Include(q => q.Request)
                .ThenInclude(r => r.Beneficiary)
            .Include(q => q.Request)
                .ThenInclude(r => r.Specialty)
            .Include(q => q.Request)
                .ThenInclude(r => r.Consultation)
            .FirstOrDefaultAsync(q => q.Id == qrCodeTokenId && !q.IsDeleted, ct);

        if (qr is null) return UiActionResult.Fail("رمز QR غير صحيح أو غير موجود.");
        if (qr.ExpiresAt < DateTime.UtcNow) return UiActionResult.Fail("رمز QR منتهي الصلاحية.");

        var request = qr.Request;

        if (request.ServiceType != ServiceRequestType.MedicalConsultation)
            return UiActionResult.Fail("هذا QR تابع لصيدلية أو معمل أو مركز أشعة ولا يمكن للطبيب استخدامه.");

        if (qr.IsUsed && request.Consultation is not null)
        {
            return UiActionResult.Fail("تم استخدام رمز QR من قبل.");
        }

        if (qr.IsUsed && request.Status == RequestStatus.Approved && request.Consultation is null)
        {
            qr.IsUsed = false;
            qr.UsedAt = null;
            qr.UsedByDoctorId = null;
        }

        if (request.Status != RequestStatus.Approved)
            return UiActionResult.Fail("هذا الطلب غير معتمد للكشف.");

        var doctor = await db.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == doctorUserId && d.IsAvailable && !d.IsDeleted, ct);

        if (doctor is null)
            return UiActionResult.Fail("حساب الطبيب غير متاح أو غير نشط.");

        if (request.SpecialtyId is null || doctor.SpecialtyId != request.SpecialtyId.Value)
            return UiActionResult.Fail("هذا الطلب تابع لتخصص آخر ولا يمكن للطبيب الحالي إنهاؤه.");

        if (request.DoctorId is not null && request.DoctorId != doctor.Id)
            return UiActionResult.Fail("هذا الطلب مخصص لطبيب آخر ولا يمكن للطبيب الحالي إنهاؤه.");

        await CompleteApprovedRequestCoreAsync(request, qr, doctorUserId, note, ct);
        return UiActionResult.Ok("تم حفظ نتيجة الاستشارة باستخدام QR وتحديث حالة الطلب.");
    }

    private async Task CompleteApprovedRequestCoreAsync(MedicalRequest request, QRCodeToken? qr, string doctorUserId, string? note, CancellationToken ct)
    {
        var finalNote = string.IsNullOrWhiteSpace(note)
            ? "تمت الاستشارة."
            : note.Trim();

        request.Status = RequestStatus.Completed;
        request.ReviewNoteAr = string.IsNullOrWhiteSpace(request.ReviewNoteAr)
            ? $"نتيجة الطبيب: {finalNote}"
            : $"{request.ReviewNoteAr}\nنتيجة الطبيب: {finalNote}";

        if (qr is not null)
        {
            qr.IsUsed = true;
            qr.UsedAt = DateTime.UtcNow;
            qr.UsedByDoctorId = doctorUserId;
        }

        if (request.Consultation is null && qr is not null)
        {
            db.Consultations.Add(new Consultation
            {
                RequestId = request.Id,
                QRCodeTokenId = qr.Id,
                DoctorId = doctorUserId,
                DiagnosisAr = finalNote,
                DiagnosisEn = string.Empty,
                NotesAr = finalNote,
                ConsultedAt = DateTime.UtcNow
            });
        }
        else if (request.Consultation is not null)
        {
            request.Consultation.DoctorId = doctorUserId;
            request.Consultation.DiagnosisAr = finalNote;
            request.Consultation.NotesAr = finalNote;
            request.Consultation.ConsultedAt = DateTime.UtcNow;
        }

        AddNotification(
            request.Beneficiary.UserId,
            NotificationType.ConsultationCompleted,
            "تم تسجيل نتيجة الكشف",
            "تم تسجيل نتيجة الكشف بواسطة الطبيب. يمكنك مراجعة حالة طلبك من صفحة طلباتي.",
            "/portal/requests",
            "MedicalRequest",
            request.Id.ToString()
        );

        AddAuditLog(
            doctorUserId,
            "Consultation.Completed",
            "MedicalRequest",
            request.Id.ToString(),
            null,
            $"DoctorUserId={doctorUserId}; QRCodeTokenId={qr?.Id}; Note={finalNote}");

        if (qr is not null)
        {
            AddAuditLog(
                doctorUserId,
                "QR.Used",
                "QRCodeToken",
                qr.Id.ToString(),
                null,
                $"RequestId={request.Id}; UsedAt={qr.UsedAt:O}");
        }

        await db.SaveChangesAsync(ct);
    }



    public async Task<int> NormalizeActiveQrExpiryAsync(CancellationToken ct = default)
    {
        var tokens = await db.QRCodeTokens
            .Include(q => q.Request)
            .Where(q =>
                !q.IsDeleted
                && !q.IsUsed
                && q.Request.Status == RequestStatus.Approved
                && q.Request.AppointmentDate != null)
            .ToListAsync(ct);

        var changed = 0;

        foreach (var token in tokens)
        {
            var expectedExpiry = GetQrExpiryUtc(token.Request.AppointmentDate);

            if (Math.Abs((token.ExpiresAt - expectedExpiry).TotalSeconds) <= 1)
            {
                continue;
            }

            token.ExpiresAt = expectedExpiry;
            changed++;
        }

        if (changed > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        return changed;
    }

    private static DateTime GetQrExpiryUtc(DateOnly? appointmentDate)
    {
        if (appointmentDate is null)
        {
            return DateTime.UtcNow;
        }

        var localEndOfDay = appointmentDate.Value.ToDateTime(
            new TimeOnly(23, 59, 59, 999),
            DateTimeKind.Unspecified);

        try
        {
            var cairo = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
            return TimeZoneInfo.ConvertTimeToUtc(localEndOfDay, cairo);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.ConvertTimeToUtc(localEndOfDay, TimeZoneInfo.Local);
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.ConvertTimeToUtc(localEndOfDay, TimeZoneInfo.Local);
        }
    }

    private static bool IsWorkingOnDate(string? workingDays, DateOnly date)
    {
        if (string.IsNullOrWhiteSpace(workingDays))
        {
            return true;
        }

        var dayCode = date.DayOfWeek switch
        {
            DayOfWeek.Saturday => "Sat",
            DayOfWeek.Sunday => "Sun",
            DayOfWeek.Monday => "Mon",
            DayOfWeek.Tuesday => "Tue",
            DayOfWeek.Wednesday => "Wed",
            DayOfWeek.Thursday => "Thu",
            DayOfWeek.Friday => "Fri",
            _ => string.Empty
        };

        return workingDays
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(d => string.Equals(d, dayCode, StringComparison.OrdinalIgnoreCase));
    }

    private static string DayNameAr(DayOfWeek day) => day switch
    {
        DayOfWeek.Saturday => "السبت", DayOfWeek.Sunday => "الأحد", DayOfWeek.Monday => "الاثنين",
        DayOfWeek.Tuesday => "الثلاثاء", DayOfWeek.Wednesday => "الأربعاء", DayOfWeek.Thursday => "الخميس",
        DayOfWeek.Friday => "الجمعة", _ => string.Empty
    };

    private static string BuildApprovalNotificationBody(MedicalRequest request, Doctor? doctor)
    {
        if (request.ServiceType != ServiceRequestType.MedicalConsultation)
        {
            var providerName = request.AssignedProvider?.FullNameAr ?? "الجهة المختارة";
            if (request.AppointmentDate is not null)
            {
                return $"تم قبول {ServiceTypeAr(request.ServiceType)} لدى {providerName} ليوم {request.AppointmentDate.Value:dd/MM/yyyy}. تم توليد QR لتقديمه عند تنفيذ الخدمة.";
            }

            return $"تم قبول {ServiceTypeAr(request.ServiceType)} لدى {providerName}. تم توليد QR لتقديمه عند تنفيذ الخدمة.";
        }

        var doctorName = doctor?.User?.FullNameAr;
        var specialtyName = request.Specialty?.NameAr ?? "الاستشارة الطبية";

        var dateText = request.AppointmentDate is null
            ? "لم يتم تحديد يوم كشف"
            : request.AppointmentDate.Value.ToString("dd/MM/yyyy");

        if (string.IsNullOrWhiteSpace(doctorName))
        {
            return $"تم قبول طلب {specialtyName} وتوليد QR الخاص بالحالة. يوم الكشف: {dateText}.";
        }

        return $"تم قبول طلب {specialtyName} مع الطبيب {doctorName}. يوم الكشف: {dateText}. تم توليد QR الخاص بالحالة.";
    }

    private async Task<int> RejectOtherPendingRequestsIfDayIsFullAsync(
        Guid approvedRequestId,
        Guid doctorId,
        DateOnly appointmentDate,
        Doctor doctor,
        string reviewerId,
        CancellationToken ct)
    {
        var approvedCount = await db.MedicalRequests.CountAsync(r =>
            r.DoctorId == doctorId &&
            r.AppointmentDate == appointmentDate &&
            r.Status == RequestStatus.Approved,
            ct);

        if (approvedCount < doctor.MaxDailySlots)
        {
            return 0;
        }

        var pendingRequests = await db.MedicalRequests
            .Include(r => r.Beneficiary)
            .Include(r => r.Specialty)
            .Where(r =>
                r.Id != approvedRequestId &&
                r.DoctorId == doctorId &&
                r.AppointmentDate == appointmentDate &&
                (r.Status == RequestStatus.Submitted || r.Status == RequestStatus.UnderReview))
            .ToListAsync(ct);

        foreach (var item in pendingRequests)
        {
            item.Status = RequestStatus.Rejected;
            item.ReviewedBy = reviewerId;
            item.ReviewedAt = DateTime.UtcNow;
            item.ReviewNoteAr = "تم إلغاء الطلب تلقائيًا لأن عدد كشوفات الطبيب في هذا اليوم اكتمل.";

            AddNotification(
                item.Beneficiary.UserId,
                NotificationType.RequestRejected,
                "تم إلغاء طلبك تلقائيًا",
                $"تم إلغاء طلب {item.Specialty?.NameAr ?? "الاستشارة الطبية"} لأن يوم الطبيب المختار اكتمل. يمكنك تقديم طلب جديد في يوم آخر.",
                "/portal/requests",
                "MedicalRequest",
                item.Id.ToString()
            );

            AddAuditLog(
                reviewerId,
                "Request.AutoRejected.CapacityFull",
                "MedicalRequest",
                item.Id.ToString(),
                null,
                $"DoctorId={doctorId}; AppointmentDate={appointmentDate}; MaxDailySlots={doctor.MaxDailySlots}");
        }

        return pendingRequests.Count;
    }


    private static string AppointmentRequiredMessage(ServiceRequestType type) => type switch
    {
        ServiceRequestType.PharmacyMedication => "اختر يوم استلام العلاج.",
        ServiceRequestType.LaboratoryTest => "اختر يوم إجراء التحاليل.",
        ServiceRequestType.RadiologyScan => "اختر يوم إجراء الأشعة.",
        _ => "اختر يوم تنفيذ الخدمة."
    };

    private static string AppointmentApprovalRequiredMessage(ServiceRequestType type) => type switch
    {
        ServiceRequestType.PharmacyMedication => "يجب اختيار يوم استلام العلاج قبل الموافقة.",
        ServiceRequestType.LaboratoryTest => "يجب اختيار يوم إجراء التحاليل قبل الموافقة.",
        ServiceRequestType.RadiologyScan => "يجب اختيار يوم إجراء الأشعة قبل الموافقة.",
        _ => "يجب اختيار يوم تنفيذ الخدمة قبل الموافقة."
    };

    private static string ProviderNameAr(ServiceRequestType type) => type switch
    {
        ServiceRequestType.PharmacyMedication => "الصيدلية",
        ServiceRequestType.LaboratoryTest => "المعمل",
        ServiceRequestType.RadiologyScan => "مركز الأشعة",
        _ => "الجهة"
    };

    public static string ServiceTypeAr(ServiceRequestType serviceType) => serviceType switch
    {
        ServiceRequestType.MedicalConsultation => "طلب كشف طبي",
        ServiceRequestType.PharmacyMedication => "طلب علاج من صيدلية",
        ServiceRequestType.LaboratoryTest => "طلب تحاليل طبية",
        ServiceRequestType.RadiologyScan => "طلب أشعة",
        _ => "طلب خدمة صحية"
    };

    public static string ServiceTypeIcon(ServiceRequestType serviceType) => serviceType switch
    {
        ServiceRequestType.MedicalConsultation => "🩺",
        ServiceRequestType.PharmacyMedication => "💊",
        ServiceRequestType.LaboratoryTest => "🧪",
        ServiceRequestType.RadiologyScan => "🩻",
        _ => "🏥"
    };


    private async Task<bool> IsActiveProviderForServiceAsync(
        string providerUserId,
        ServiceRequestType serviceType,
        CancellationToken ct)
    {
        var expectedRoles = ExpectedProviderRoles(serviceType);

        return await (
            from user in db.Users
            join userRole in db.UserRoles on user.Id equals userRole.UserId
            join role in db.Roles on userRole.RoleId equals role.Id
            where user.Id == providerUserId
                  && user.IsActive
                  && role.Name != null
                  && expectedRoles.Contains(role.Name)
            select user.Id
        ).AnyAsync(ct);
    }

    private static string BuildPartnerCompletionNote(
        ServiceRequestType serviceType,
        PartnerQrCompletionInput input)
    {
        var lines = new List<string>();

        switch (serviceType)
        {
            case ServiceRequestType.PharmacyMedication:
                lines.Add($"الأدوية المصروفة: {input.PrimaryResult.Trim()}");
                if (!string.IsNullOrWhiteSpace(input.ReferenceNumber))
                    lines.Add($"رقم الصرف / الوصفة: {input.ReferenceNumber.Trim()}");
                if (input.ExpectedDeliveryAt is not null)
                    lines.Add($"موعد الاستلام: {input.ExpectedDeliveryAt.Value.ToLocalTime():yyyy/MM/dd HH:mm}");
                break;

            case ServiceRequestType.LaboratoryTest:
                lines.Add($"التحاليل التي تم تنفيذها: {input.PrimaryResult.Trim()}");
                if (!string.IsNullOrWhiteSpace(input.ReferenceNumber))
                    lines.Add($"رقم العينة / المرجع: {input.ReferenceNumber.Trim()}");
                if (input.ExpectedDeliveryAt is not null)
                    lines.Add($"موعد تسليم النتيجة: {input.ExpectedDeliveryAt.Value.ToLocalTime():yyyy/MM/dd HH:mm}");
                break;

            case ServiceRequestType.RadiologyScan:
                lines.Add($"الأشعة التي تم تنفيذها: {input.PrimaryResult.Trim()}");
                if (!string.IsNullOrWhiteSpace(input.ReferenceNumber))
                    lines.Add($"رقم الفحص / المرجع: {input.ReferenceNumber.Trim()}");
                if (input.ExpectedDeliveryAt is not null)
                    lines.Add($"موعد تسليم التقرير: {input.ExpectedDeliveryAt.Value.ToLocalTime():yyyy/MM/dd HH:mm}");
                break;
        }

        if (!string.IsNullOrWhiteSpace(input.AdditionalNotes))
        {
            lines.Add($"ملاحظات الجهة: {input.AdditionalNotes.Trim()}");
        }

        lines.Add($"تم التنفيذ بتاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}");
        return string.Join(Environment.NewLine, lines);
    }

    private static string PrimaryResultRequiredMessage(ServiceRequestType serviceType) => serviceType switch
    {
        ServiceRequestType.PharmacyMedication => "اكتب الأدوية التي تم صرفها للمستفيد.",
        ServiceRequestType.LaboratoryTest => "اكتب التحاليل التي تم تنفيذها للمستفيد.",
        ServiceRequestType.RadiologyScan => "اكتب نوع الأشعة التي تم تنفيذها للمستفيد.",
        _ => "اكتب نتيجة تنفيذ الخدمة."
    };

    private static decimal NormalizeDiscount(decimal value) => Math.Clamp(value, 0m, 100m);

    private static string PartnerCompletionSuccessMessage(ServiceRequestType serviceType) => serviceType switch
    {
        ServiceRequestType.PharmacyMedication => "تم صرف العلاج واستخدام QR وإبلاغ المستفيد بنجاح.",
        ServiceRequestType.LaboratoryTest => "تم تسجيل تنفيذ التحاليل واستخدام QR وإبلاغ المستفيد بنجاح.",
        ServiceRequestType.RadiologyScan => "تم تسجيل تنفيذ الأشعة واستخدام QR وإبلاغ المستفيد بنجاح.",
        _ => "تم تنفيذ الخدمة واستخدام QR وإبلاغ المستفيد بنجاح."
    };

    private static string[] ExpectedProviderRoles(ServiceRequestType serviceType) => serviceType switch
    {
        ServiceRequestType.PharmacyMedication => ["Pharmacist", "Pharmacy"],
        ServiceRequestType.LaboratoryTest => ["Laboratory"],
        ServiceRequestType.RadiologyScan => ["RadiologyCenter"],
        _ => []
    };

    private static string DefaultApprovalNote(ServiceRequestType serviceType) => serviceType switch
    {
        ServiceRequestType.MedicalConsultation => "تمت الموافقة على الطلب. برجاء التوجه للطبيب في اليوم المحدد بعد التنسيق.",
        ServiceRequestType.PharmacyMedication => "تمت الموافقة على صرف العلاج. برجاء التوجه للصيدلية المختارة ومعك QR.",
        ServiceRequestType.LaboratoryTest => "تمت الموافقة على طلب التحاليل. برجاء التوجه للمعمل المختار ومعك QR.",
        ServiceRequestType.RadiologyScan => "تمت الموافقة على طلب الأشعة. برجاء التوجه لمركز الأشعة المختار ومعك QR.",
        _ => "تمت الموافقة على الطلب."
    };

    private static string DefaultCompletionNote(ServiceRequestType serviceType) => serviceType switch
    {
        ServiceRequestType.PharmacyMedication => "تم صرف العلاج للمستفيد.",
        ServiceRequestType.LaboratoryTest => "تم إجراء التحاليل للمستفيد.",
        ServiceRequestType.RadiologyScan => "تم إجراء الأشعة للمستفيد.",
        _ => "تم تنفيذ الخدمة للمستفيد."
    };

    private static string CompletionTitle(ServiceRequestType serviceType) => serviceType switch
    {
        ServiceRequestType.PharmacyMedication => "تم صرف العلاج",
        ServiceRequestType.LaboratoryTest => "تم إجراء التحاليل",
        ServiceRequestType.RadiologyScan => "تم إجراء الأشعة",
        _ => "تم تنفيذ الخدمة"
    };

    private void AddAuditLog(
        string? userId,
        string action,
        string entityType,
        string? entityId,
        string? oldValues = null,
        string? newValues = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues
        });
    }

    private void AddNotification(
        string userId,
        NotificationType type,
        string title,
        string body,
        string? url,
        string? entityType,
        string? entityId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            TitleAr = title,
            BodyAr = body,
            Url = url,
            EntityType = entityType,
            EntityId = entityId,
            IsRead = false
        };

        db.Notifications.Add(notification);

        AddAuditLog(
            userId,
            "Notification.Created",
            "Notification",
            notification.Id.ToString(),
            null,
            $"Type={type}; Title={title}; EntityType={entityType}; EntityId={entityId}");
    }

    public static string StatusAr(RequestStatus status) => status switch
    {
        RequestStatus.Draft => "مسودة",
        RequestStatus.Submitted => "تم التقديم",
        RequestStatus.UnderReview => "قيد المراجعة",
        RequestStatus.Approved => "مقبول",
        RequestStatus.Rejected => "مرفوض",
        RequestStatus.Completed => "مكتمل",
        _ => "غير معروف"
    };

    public static string StatusCss(RequestStatus status) => status switch
    {
        RequestStatus.Submitted => "info",
        RequestStatus.UnderReview => "warning",
        RequestStatus.Approved => "success",
        RequestStatus.Rejected => "danger",
        RequestStatus.Completed => "teal",
        _ => "muted"
    };

    public static string NotificationIcon(NotificationType type) => type switch
    {
        NotificationType.RequestApproved => "✅",
        NotificationType.RequestRejected => "❌",
        NotificationType.DocumentRequired => "📎",
        NotificationType.AppointmentReminder => "📅",
        NotificationType.ConsultationCompleted => "🩺",
        NotificationType.ServiceCompleted => "✅",
        _ => "🔔"
    };

    public static string NotificationIconBackground(NotificationType type) => type switch
    {
        NotificationType.RequestApproved => "#dcfce7",
        NotificationType.RequestRejected => "#fee2e2",
        NotificationType.DocumentRequired => "#fffbeb",
        NotificationType.AppointmentReminder => "#eff6ff",
        NotificationType.ConsultationCompleted => "#ecfdf3",
        NotificationType.ServiceCompleted => "#ecfdf3",
        _ => "#f1f5f9"
    };

    public static string NotificationIconColor(NotificationType type) => type switch
    {
        NotificationType.RequestApproved => "#15803d",
        NotificationType.RequestRejected => "#b91c1c",
        NotificationType.DocumentRequired => "#b45309",
        NotificationType.AppointmentReminder => "#2563eb",
        NotificationType.ConsultationCompleted => "#047857",
        NotificationType.ServiceCompleted => "#047857",
        _ => "#475467"
    };
}
