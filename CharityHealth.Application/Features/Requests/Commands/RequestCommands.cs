using CharityHealth.Application.Interfaces.Services;
using CharityHealth.Domain.Entities;
using CharityHealth.Domain.Enums;
using CharityHealth.Domain.Interfaces.Repositories;
using CharityHealth.Shared.Wrappers;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CharityHealth.Application.Features.Requests.Commands;

// ── Submit Request ─────────────────────────────────────
public record SubmitRequestCommand(
    string BeneficiaryUserId,
    Guid SpecialtyId,
    string? DescriptionAr,
    Guid? DoctorId = null,
    DateOnly? AppointmentDate = null
) : IRequest<Result<Guid>>;

public class SubmitRequestValidator : AbstractValidator<SubmitRequestCommand>
{
    public SubmitRequestValidator()
    {
        RuleFor(x => x.BeneficiaryUserId).NotEmpty();
        RuleFor(x => x.SpecialtyId).NotEqual(Guid.Empty).WithMessage("يجب اختيار التخصص الطبي");

        RuleFor(x => x.DoctorId)
            .Must(id => id is null || id.Value != Guid.Empty)
            .WithMessage("يجب اختيار الطبيب بشكل صحيح");

        RuleFor(x => x.AppointmentDate)
            .Must(date => date is null || date.Value >= DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("لا يمكن اختيار يوم سابق");

        RuleFor(x => x.DescriptionAr)
            .MaximumLength(2000).WithMessage("الوصف لا يتجاوز 2000 حرف")
            .When(x => !string.IsNullOrEmpty(x.DescriptionAr));
    }
}

public class SubmitRequestHandler(
    IUnitOfWork uow,
    IAuditService audit,
    INotificationSender notifications,
    ILogger<SubmitRequestHandler> logger)
    : IRequestHandler<SubmitRequestCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SubmitRequestCommand cmd, CancellationToken ct)
    {
        var beneficiary = await uow.Beneficiaries
            .FirstOrDefaultAsync(b => b.UserId == cmd.BeneficiaryUserId, ct);

        if (beneficiary is null)
            return Result<Guid>.Failure("لم يتم العثور على بيانات المستفيد");

        if (!await uow.Specialties.ExistsAsync(s => s.Id == cmd.SpecialtyId && s.IsActive, ct))
            return Result<Guid>.Failure("التخصص المختار غير متاح");

        Doctor? selectedDoctor = null;

        if (cmd.DoctorId is not null)
        {
            selectedDoctor = await uow.Doctors
                .FirstOrDefaultAsync(d =>
                    d.Id == cmd.DoctorId.Value &&
                    d.IsAvailable &&
                    !d.IsDeleted,
                    ct);

            if (selectedDoctor is null)
                return Result<Guid>.Failure("الطبيب المختار غير متاح حالياً");

            if (selectedDoctor.SpecialtyId != cmd.SpecialtyId)
                return Result<Guid>.Failure("الطبيب المختار لا يتبع التخصص المحدد");

            if (cmd.AppointmentDate is null)
                return Result<Guid>.Failure("يجب اختيار يوم الكشف");

            if (!IsDoctorWorkingOnDate(selectedDoctor.WorkingDays, cmd.AppointmentDate.Value))
                return Result<Guid>.Failure("الطبيب غير متاح في اليوم المختار");
        }

        var hasActiveRequest = await uow.MedicalRequests.ExistsAsync(
            r => r.BeneficiaryId == beneficiary.Id
              && r.SpecialtyId == cmd.SpecialtyId
              && (r.Status == RequestStatus.Submitted
                  || r.Status == RequestStatus.UnderReview
                  || r.Status == RequestStatus.Approved),
            ct);

        if (hasActiveRequest)
            return Result<Guid>.Failure("لديك طلب قيد المراجعة لنفس التخصص. انتظر إتمامه أولاً.");

        var request = new MedicalRequest
        {
            BeneficiaryId = beneficiary.Id,
            SpecialtyId = cmd.SpecialtyId,
            DoctorId = cmd.DoctorId,
            AppointmentDate = cmd.AppointmentDate,
            Status = RequestStatus.Submitted,
            DescriptionAr = cmd.DescriptionAr,
            SubmittedAt = DateTime.UtcNow,
        };

        await uow.MedicalRequests.AddAsync(request, ct);
        await uow.SaveChangesAsync(ct);

        await audit.LogAsync("Request.Submitted", "MedicalRequest", request.Id.ToString(),
            newValues: $"{{\"specialtyId\":\"{cmd.SpecialtyId}\",\"doctorId\":\"{cmd.DoctorId}\",\"appointmentDate\":\"{cmd.AppointmentDate}\",\"beneficiaryId\":\"{beneficiary.Id}\"}}");

        await notifications.SendToRoleGroupAsync("Staff", "NewRequestSubmitted", new
        {
            requestId = request.Id,
            beneficiaryName = beneficiary.User?.FullNameAr ?? "مستفيد",
            submittedAt = request.SubmittedAt,
            doctorId = request.DoctorId,
            appointmentDate = request.AppointmentDate
        });

        logger.LogInformation("Request {RequestId} submitted by beneficiary {BeneficiaryId}",
            request.Id, beneficiary.Id);

        return Result<Guid>.Success(request.Id, "تم تقديم طلبك بنجاح وهو الآن قيد المراجعة");
    }

    private static bool IsDoctorWorkingOnDate(string? workingDays, DateOnly date)
    {
        if (string.IsNullOrWhiteSpace(workingDays))
        {
            return true;
        }

        var code = date.DayOfWeek switch
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
            .Any(x => string.Equals(x, code, StringComparison.OrdinalIgnoreCase));
    }
}

// ── Upload Document Command ────────────────────────────
public record UploadDocumentCommand(
    Guid RequestId,
    string BeneficiaryUserId,
    Stream FileStream,
    string FileName,
    long FileSizeBytes,
    DocumentType DocumentType
) : IRequest<Result<Guid>>;

public class UploadDocumentHandler(
    IUnitOfWork uow,
    IFileStorageService fileStorage,
    IAuditService audit,
    ILogger<UploadDocumentHandler> logger)
    : IRequestHandler<UploadDocumentCommand, Result<Guid>>
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png"];

    public async Task<Result<Guid>> Handle(UploadDocumentCommand cmd, CancellationToken ct)
    {
        // Validate file size
        if (cmd.FileSizeBytes > MaxFileSizeBytes)
            return Result<Guid>.Failure("حجم الملف يتجاوز الحد المسموح به (5 ميجابايت)");

        // Validate file extension
        var ext = Path.GetExtension(cmd.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return Result<Guid>.Failure("نوع الملف غير مسموح به. الأنواع المقبولة: PDF, JPG, PNG");

        // Verify request belongs to this beneficiary
        var request = await uow.MedicalRequests.GetByIdAsync(cmd.RequestId, ct);
        if (request is null)
            return Result<Guid>.Failure("الطلب غير موجود");

        var beneficiary = await uow.Beneficiaries
            .FirstOrDefaultAsync(b => b.UserId == cmd.BeneficiaryUserId, ct);

        if (beneficiary is null || request.BeneficiaryId != beneficiary.Id)
            return Result<Guid>.Failure("غير مصرح لك برفع مستندات لهذا الطلب");

        // Only allow upload if request is in Draft or Submitted status
        if (request.Status != RequestStatus.Draft && request.Status != RequestStatus.Submitted)
            return Result<Guid>.Failure("لا يمكن رفع مستندات لطلب في هذه المرحلة");

        // Save file
        var folder = $"requests/{cmd.RequestId}";
        var storedPath = await fileStorage.SaveAsync(cmd.FileStream, cmd.FileName, folder, ct);

        var doc = new RequestDocument
        {
            RequestId = cmd.RequestId,
            FileName = cmd.FileName,
            FilePath = storedPath,
            FileSizeBytes = cmd.FileSizeBytes,
            DocumentType = cmd.DocumentType,
        };

        await uow.RequestDocuments.AddAsync(doc, ct);
        await uow.SaveChangesAsync(ct);

        await audit.LogAsync("Document.Uploaded", "RequestDocument", doc.Id.ToString(),
            newValues: $"{{\"requestId\":\"{cmd.RequestId}\",\"fileName\":\"{cmd.FileName}\"}}");

        logger.LogInformation("Document {DocId} uploaded for request {RequestId}", doc.Id, cmd.RequestId);

        return Result<Guid>.Success(doc.Id, "تم رفع المستند بنجاح");
    }
}
