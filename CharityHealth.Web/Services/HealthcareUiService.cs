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
    int? DoctorMaxDailySlots = null
);

public record DoctorListItem(
    Guid Id,
    string UserId,
    string FullNameAr,
    string PhoneNumber,
    string SpecialtyName,
    string LicenseNumber,
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
                r.Specialty.NameAr,
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
                r.Doctor == null ? null : r.Doctor.MaxDailySlots
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
                r.Specialty.NameAr,
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
                r.Doctor == null ? null : r.Doctor.MaxDailySlots
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
                (r.Status == RequestStatus.Approved || r.Status == RequestStatus.UnderReview)
            )
            .Include(r => r.Specialty)
            .Include(r => r.Doctor)
                .ThenInclude(d => d!.User)
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
                r.Specialty.NameAr,
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
                r.Doctor == null ? null : r.Doctor.MaxDailySlots
            ))
            .ToListAsync(ct);
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
                d.LicenseNumber,
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
            .Select(r => r.Specialty.NameAr)
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
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (request is null) return UiActionResult.Fail("الطلب غير موجود");
        if (request.Status == RequestStatus.Completed) return UiActionResult.Fail("الطلب مكتمل بالفعل");
        if (request.Status == RequestStatus.Rejected) return UiActionResult.Fail("لا يمكن الموافقة على طلب مرفوض إلا بعد إعادة تقديمه.");

        Doctor? doctor = request.Doctor;

        if (request.DoctorId is not null || request.AppointmentDate is not null)
        {
            if (request.DoctorId is null || request.AppointmentDate is null)
            {
                return UiActionResult.Fail("الطلب غير مكتمل: يجب أن يحتوي على طبيب ويوم كشف.");
            }

            doctor = await db.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == request.DoctorId.Value && d.IsAvailable && !d.IsDeleted, ct);

            if (doctor is null)
            {
                return UiActionResult.Fail("الطبيب المختار غير متاح أو تم حذفه.");
            }

            if (doctor.SpecialtyId != request.SpecialtyId)
            {
                return UiActionResult.Fail("الطبيب المختار لا يتبع تخصص الطلب.");
            }

            if (!IsDoctorWorkingOnDate(doctor.WorkingDays, request.AppointmentDate.Value))
            {
                return UiActionResult.Fail("الطبيب غير متاح في اليوم المختار.");
            }

            var approvedCountBefore = await db.MedicalRequests.CountAsync(r =>
                r.Id != request.Id &&
                r.DoctorId == request.DoctorId &&
                r.AppointmentDate == request.AppointmentDate &&
                r.Status == RequestStatus.Approved,
                ct);

            if (approvedCountBefore >= doctor.MaxDailySlots)
            {
                return UiActionResult.Fail("هذا اليوم مكتمل للطبيب المختار ولا يمكن قبول طلبات إضافية.");
            }
        }

        request.Status = RequestStatus.Approved;
        request.ReviewedBy = reviewerId;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewNoteAr = string.IsNullOrWhiteSpace(note)
            ? "تمت الموافقة على الطلب. برجاء التوجه للطبيب في اليوم المحدد بعد التنسيق."
            : note.Trim();

        var qrWasCreated = request.QRCodeToken is null;

        if (request.QRCodeToken is null)
        {
            var raw = $"REQ:{request.Id}:BEN:{request.BeneficiaryId}:EXP:{DateTime.UtcNow.AddDays(7):O}:{Guid.NewGuid():N}";
            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

            db.QRCodeTokens.Add(new QRCodeToken
            {
                RequestId = request.Id,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsUsed = false
            });
        }
        else
        {
            request.QRCodeToken.ExpiresAt = DateTime.UtcNow.AddDays(7);
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

        if (request.DoctorId is not null && request.AppointmentDate is not null && doctor is not null)
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
            $"Status={request.Status}; BeneficiaryId={request.BeneficiaryId}; SpecialtyId={request.SpecialtyId}; DoctorId={request.DoctorId}; AppointmentDate={request.AppointmentDate}");

        AddAuditLog(
            reviewerId,
            qrWasCreated ? "QR.Generated" : "QR.Renewed",
            "QRCodeToken",
            request.Id.ToString(),
            null,
            $"RequestId={request.Id}; ExpiresAt={DateTime.UtcNow.AddDays(7):O}");

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
            $"تم رفض طلب {request.Specialty.NameAr}. يمكنك مراجعة تفاصيل الطلب من صفحة طلباتي.",
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

        if (doctor.SpecialtyId != request.SpecialtyId)
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


    private static bool IsDoctorWorkingOnDate(string? workingDays, DateOnly date)
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

    private static string BuildApprovalNotificationBody(MedicalRequest request, Doctor? doctor)
    {
        var doctorName = doctor?.User?.FullNameAr;

        var dateText = request.AppointmentDate is null
            ? "لم يتم تحديد يوم كشف"
            : request.AppointmentDate.Value.ToString("dd/MM/yyyy");

        if (string.IsNullOrWhiteSpace(doctorName))
        {
            return $"تم قبول طلب {request.Specialty.NameAr} وتوليد QR الخاص بالحالة. يوم الكشف: {dateText}.";
        }

        return $"تم قبول طلب {request.Specialty.NameAr} مع الطبيب {doctorName}. يوم الكشف: {dateText}. تم توليد QR الخاص بالحالة.";
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
                $"تم إلغاء طلب {item.Specialty.NameAr} لأن يوم الطبيب المختار اكتمل. يمكنك تقديم طلب جديد في يوم آخر.",
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
        _ => "🔔"
    };

    public static string NotificationIconBackground(NotificationType type) => type switch
    {
        NotificationType.RequestApproved => "#dcfce7",
        NotificationType.RequestRejected => "#fee2e2",
        NotificationType.DocumentRequired => "#fffbeb",
        NotificationType.AppointmentReminder => "#eff6ff",
        NotificationType.ConsultationCompleted => "#ecfdf3",
        _ => "#f1f5f9"
    };

    public static string NotificationIconColor(NotificationType type) => type switch
    {
        NotificationType.RequestApproved => "#15803d",
        NotificationType.RequestRejected => "#b91c1c",
        NotificationType.DocumentRequired => "#b45309",
        NotificationType.AppointmentReminder => "#2563eb",
        NotificationType.ConsultationCompleted => "#047857",
        _ => "#475467"
    };
}
