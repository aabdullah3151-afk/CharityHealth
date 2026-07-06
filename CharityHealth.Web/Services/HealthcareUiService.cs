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
    bool HasQrCode
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

        var approved = await db.MedicalRequests.CountAsync(
            r => r.Status == RequestStatus.Approved,
            ct
        );

        var rejected = await db.MedicalRequests.CountAsync(
            r => r.Status == RequestStatus.Rejected,
            ct
        );

        var completed = await db.MedicalRequests.CountAsync(
            r => r.Status == RequestStatus.Completed,
            ct
        );

        var specialties = await db.Specialties.CountAsync(
            s => s.IsActive,
            ct
        );

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
                r.QRCodeToken != null
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
                r.QRCodeToken != null
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
                r.SpecialtyId == doctor.SpecialtyId &&
                (r.Status == RequestStatus.Approved || r.Status == RequestStatus.UnderReview)
            )
            .Include(r => r.Specialty)
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
                r.QRCodeToken != null
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
        {
            return UiActionResult.Fail("اسم التخصص بالعربي مطلوب.");
        }

        if (string.IsNullOrWhiteSpace(nameEn))
        {
            return UiActionResult.Fail("اسم التخصص بالإنجليزي مطلوب.");
        }

        var exists = await db.Specialties.AnyAsync(s =>
            s.NameAr == nameAr || s.NameEn == nameEn,
            ct
        );

        if (exists)
        {
            return UiActionResult.Fail("هذا التخصص موجود بالفعل.");
        }

        var specialty = new Specialty
        {
            NameAr = nameAr,
            NameEn = nameEn,
            DescriptionAr = descriptionAr,
            DescriptionEn = descriptionEn,
            IsActive = isActive
        };

        db.Specialties.Add(specialty);
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
        {
            return UiActionResult.Fail("لم يتم تحديد التخصص المطلوب تعديله.");
        }

        if (string.IsNullOrWhiteSpace(nameAr))
        {
            return UiActionResult.Fail("اسم التخصص بالعربي مطلوب.");
        }

        if (string.IsNullOrWhiteSpace(nameEn))
        {
            return UiActionResult.Fail("اسم التخصص بالإنجليزي مطلوب.");
        }

        var specialty = await db.Specialties.FirstOrDefaultAsync(s => s.Id == id, ct);

        if (specialty is null)
        {
            return UiActionResult.Fail("التخصص غير موجود أو تم حذفه.");
        }

        var exists = await db.Specialties.AnyAsync(s =>
            s.Id != id && (s.NameAr == nameAr || s.NameEn == nameEn),
            ct
        );

        if (exists)
        {
            return UiActionResult.Fail("يوجد تخصص آخر بنفس الاسم العربي أو الإنجليزي.");
        }

        specialty.NameAr = nameAr;
        specialty.NameEn = nameEn;
        specialty.DescriptionAr = descriptionAr;
        specialty.DescriptionEn = descriptionEn;
        specialty.IsActive = isActive;

        await db.SaveChangesAsync(ct);

        return UiActionResult.Ok("تم تعديل التخصص بنجاح.");
    }

    public async Task<UiActionResult> DeleteSpecialtyAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return UiActionResult.Fail("لم يتم تحديد التخصص المطلوب حذفه.");
        }

        var specialty = await db.Specialties
            .Include(s => s.Doctors)
            .Include(s => s.MedicalRequests)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (specialty is null)
        {
            return UiActionResult.Fail("التخصص غير موجود أو تم حذفه بالفعل.");
        }

        if (specialty.Doctors.Count > 0 || specialty.MedicalRequests.Count > 0)
        {
            return UiActionResult.Fail("لا يمكن حذف هذا التخصص لأنه مرتبط بأطباء أو طلبات. يمكنك تعطيله من زر التعديل بدل الحذف.");
        }

        db.Specialties.Remove(specialty);
        await db.SaveChangesAsync(ct);

        return UiActionResult.Ok("تم حذف التخصص بنجاح.");
    }

    public async Task<List<AuditLogListItem>> GetAuditLogsAsync(CancellationToken ct = default)
    {
        return await db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.Timestamp)
            .Take(100)
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

        var currentMonthStart = new DateTime(
            now.Year,
            now.Month,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc
        );

        var start = currentMonthStart.AddMonths(-(months - 1));

        var rows = await db.MedicalRequests
            .AsNoTracking()
            .Where(r => r.SubmittedAt >= start)
            .Select(r => new
            {
                r.SubmittedAt,
                r.Status
            })
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
                    .Where(r =>
                        r.SubmittedAt.Year == monthDate.Year &&
                        r.SubmittedAt.Month == monthDate.Month
                    )
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
                .Select((name, index) => new SpecialtyDashboardPoint(
                    name,
                    0,
                    0,
                    palette[index % palette.Length]
                ))
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

    public async Task<UiActionResult> MarkUnderReviewAsync(Guid requestId, string reviewerId, CancellationToken ct = default)
    {
        var request = await db.MedicalRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);
        if (request is null) return UiActionResult.Fail("الطلب غير موجود");

        if (request.Status is RequestStatus.Approved or RequestStatus.Rejected or RequestStatus.Completed)
        {
            return UiActionResult.Fail("لا يمكن نقل هذا الطلب للمراجعة في حالته الحالية");
        }

        request.Status = RequestStatus.UnderReview;
        request.ReviewedBy = reviewerId;
        request.ReviewedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return UiActionResult.Ok("تم نقل الطلب إلى قيد المراجعة");
    }

    public async Task<UiActionResult> ApproveRequestAsync(Guid requestId, string reviewerId, string? note, CancellationToken ct = default)
    {
        var request = await db.MedicalRequests
            .Include(r => r.QRCodeToken)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (request is null) return UiActionResult.Fail("الطلب غير موجود");
        if (request.Status == RequestStatus.Completed) return UiActionResult.Fail("الطلب مكتمل بالفعل");

        request.Status = RequestStatus.Approved;
        request.ReviewedBy = reviewerId;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewNoteAr = string.IsNullOrWhiteSpace(note)
            ? "تمت الموافقة على الطلب. برجاء التوجه للطبيب بعد التنسيق."
            : note.Trim();

        if (request.QRCodeToken is null)
        {
            var raw = $"REQ:{request.Id}:BEN:{request.BeneficiaryId}:EXP:{DateTime.UtcNow.AddDays(7):O}";
            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

            db.QRCodeTokens.Add(new QRCodeToken
            {
                RequestId = request.Id,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsUsed = false
            });
        }

        await db.SaveChangesAsync(ct);
        return UiActionResult.Ok("تمت الموافقة على الطلب وتوليد رمز تحقق للحالة");
    }

    public async Task<UiActionResult> RejectRequestAsync(Guid requestId, string reviewerId, string? note, CancellationToken ct = default)
    {
        var request = await db.MedicalRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);
        if (request is null) return UiActionResult.Fail("الطلب غير موجود");
        if (request.Status == RequestStatus.Completed) return UiActionResult.Fail("لا يمكن رفض طلب مكتمل");

        request.Status = RequestStatus.Rejected;
        request.ReviewedBy = reviewerId;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewNoteAr = string.IsNullOrWhiteSpace(note)
            ? "تم رفض الطلب بعد المراجعة."
            : note.Trim();

        await db.SaveChangesAsync(ct);
        return UiActionResult.Ok("تم رفض الطلب وتسجيل سبب القرار");
    }

    public async Task<UiActionResult> CompleteRequestAsync(Guid requestId, string doctorUserId, string? note, CancellationToken ct = default)
    {
        var request = await db.MedicalRequests
            .Include(r => r.QRCodeToken)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (request is null) return UiActionResult.Fail("الطلب غير موجود");
        if (request.Status != RequestStatus.Approved) return UiActionResult.Fail("لا يمكن إنهاء الطلب قبل الموافقة عليه");

        request.Status = RequestStatus.Completed;
        request.ReviewNoteAr = string.IsNullOrWhiteSpace(note)
            ? request.ReviewNoteAr
            : $"{request.ReviewNoteAr}\nنتيجة الطبيب: {note.Trim()}";

        if (request.QRCodeToken is not null)
        {
            request.QRCodeToken.IsUsed = true;
            request.QRCodeToken.UsedAt = DateTime.UtcNow;
            request.QRCodeToken.UsedByDoctorId = doctorUserId;
        }

        await db.SaveChangesAsync(ct);
        return UiActionResult.Ok("تم تسجيل انتهاء الكشف وتحديث حالة الطلب");
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
}
