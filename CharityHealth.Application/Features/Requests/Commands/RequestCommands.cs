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
    string? DescriptionAr
) : IRequest<Result<Guid>>;

public class SubmitRequestValidator : AbstractValidator<SubmitRequestCommand>
{
    public SubmitRequestValidator()
    {
        RuleFor(x => x.BeneficiaryUserId).NotEmpty();
        RuleFor(x => x.SpecialtyId).NotEqual(Guid.Empty).WithMessage("يجب اختيار التخصص الطبي");
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
        // Resolve beneficiary entity from user id
        var beneficiary = await uow.Beneficiaries
            .FirstOrDefaultAsync(b => b.UserId == cmd.BeneficiaryUserId, ct);

        if (beneficiary is null)
            return Result<Guid>.Failure("لم يتم العثور على بيانات المستفيد");

        // Validate specialty exists
        if (!await uow.Specialties.ExistsAsync(s => s.Id == cmd.SpecialtyId && s.IsActive, ct))
            return Result<Guid>.Failure("التخصص المختار غير متاح");

        // Check no pending approved request for same specialty
        var hasPending = await uow.MedicalRequests.ExistsAsync(
            r => r.BeneficiaryId == beneficiary.Id
              && r.SpecialtyId == cmd.SpecialtyId
              && (r.Status == RequestStatus.Submitted
                  || r.Status == RequestStatus.UnderReview
                  || r.Status == RequestStatus.Approved),
            ct);

        if (hasPending)
            return Result<Guid>.Failure("لديك طلب قيد المراجعة لنفس التخصص. انتظر إتمامه أولاً.");

        var request = new MedicalRequest
        {
            BeneficiaryId = beneficiary.Id,
            SpecialtyId = cmd.SpecialtyId,
            Status = RequestStatus.Submitted,
            DescriptionAr = cmd.DescriptionAr,
            SubmittedAt = DateTime.UtcNow,
        };

        await uow.MedicalRequests.AddAsync(request, ct);
        await uow.SaveChangesAsync(ct);

        await audit.LogAsync("Request.Submitted", "MedicalRequest", request.Id.ToString(),
            newValues: $"{{\"specialtyId\":\"{cmd.SpecialtyId}\",\"beneficiaryId\":\"{beneficiary.Id}\"}}");

        // Notify all Staff users in real-time
        await notifications.SendToRoleGroupAsync("Staff", "NewRequestSubmitted", new
        {
            requestId = request.Id,
            beneficiaryName = beneficiary.User?.FullNameAr ?? "مستفيد",
            submittedAt = request.SubmittedAt,
        });

        logger.LogInformation("Request {RequestId} submitted by beneficiary {BeneficiaryId}",
            request.Id, beneficiary.Id);

        return Result<Guid>.Success(request.Id, "تم تقديم طلبك بنجاح وهو الآن قيد المراجعة");
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
