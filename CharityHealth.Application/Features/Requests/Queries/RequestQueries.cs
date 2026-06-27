using CharityHealth.Domain.Enums;
using CharityHealth.Domain.Interfaces.Repositories;
using CharityHealth.Shared.Wrappers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CharityHealth.Application.Features.Requests.Queries;

// ── DTOs ──────────────────────────────────────────────
public record RequestSummaryDto(
    Guid Id,
    string SpecialtyNameAr,
    RequestStatus Status,
    string StatusAr,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    string? ReviewNoteAr,
    int DocumentsCount,
    bool HasQrCode
);

public record RequestDetailDto(
    Guid Id,
    string SpecialtyNameAr,
    string SpecialtyNameEn,
    RequestStatus Status,
    string StatusAr,
    string StatusColor,
    string? DescriptionAr,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    string? ReviewNoteAr,
    List<DocumentDto> Documents,
    QrCodeDto? QrCode,
    ConsultationDto? Consultation
);

public record DocumentDto(
    Guid Id,
    string FileName,
    string DocumentTypeAr,
    long FileSizeBytes,
    DateTime UploadedAt
);

public record QrCodeDto(
    Guid Id,
    DateTime ExpiresAt,
    bool IsUsed,
    bool IsExpired
);

public record ConsultationDto(
    Guid Id,
    string DoctorName,
    string DiagnosisAr,
    string? NotesAr,
    string? RecommendationsAr,
    DateTime ConsultedAt
);

// ── Get My Requests Query ──────────────────────────────
public record GetMyRequestsQuery(string BeneficiaryUserId) : IRequest<Result<List<RequestSummaryDto>>>;

public class GetMyRequestsHandler(IUnitOfWork uow)
    : IRequestHandler<GetMyRequestsQuery, Result<List<RequestSummaryDto>>>
{
    public async Task<Result<List<RequestSummaryDto>>> Handle(
        GetMyRequestsQuery q, CancellationToken ct)
    {
        var beneficiary = await uow.Beneficiaries
            .FirstOrDefaultAsync(b => b.UserId == q.BeneficiaryUserId, ct);

        if (beneficiary is null)
            return Result<List<RequestSummaryDto>>.Failure("المستفيد غير موجود");

        var requests = await uow.MedicalRequests
            .FindAsync(r => r.BeneficiaryId == beneficiary.Id, ct);

        var dtos = requests
            .OrderByDescending(r => r.SubmittedAt)
            .Select(r => new RequestSummaryDto(
                r.Id,
                r.Specialty?.NameAr ?? "—",
                r.Status,
                GetStatusAr(r.Status),
                r.SubmittedAt,
                r.ReviewedAt,
                r.ReviewNoteAr,
                r.Documents?.Count ?? 0,
                r.QRCodeToken is not null
            ))
            .ToList();

        return Result<List<RequestSummaryDto>>.Success(dtos);
    }

    private static string GetStatusAr(RequestStatus status) => status switch
    {
        RequestStatus.Draft => "مسودة",
        RequestStatus.Submitted => "تم التقديم",
        RequestStatus.UnderReview => "قيد المراجعة",
        RequestStatus.Approved => "تمت الموافقة",
        RequestStatus.Rejected => "مرفوض",
        RequestStatus.Completed => "مكتمل",
        _ => "—"
    };
}

// ── Get Request Detail Query ───────────────────────────
public record GetRequestDetailQuery(Guid RequestId, string BeneficiaryUserId)
    : IRequest<Result<RequestDetailDto>>;

public class GetRequestDetailHandler(IUnitOfWork uow)
    : IRequestHandler<GetRequestDetailQuery, Result<RequestDetailDto>>
{
    public async Task<Result<RequestDetailDto>> Handle(
        GetRequestDetailQuery q, CancellationToken ct)
    {
        var request = await uow.MedicalRequests.GetByIdAsync(q.RequestId, ct);
        if (request is null)
            return Result<RequestDetailDto>.Failure("الطلب غير موجود");

        // Security: verify ownership
        var beneficiary = await uow.Beneficiaries
            .FirstOrDefaultAsync(b => b.UserId == q.BeneficiaryUserId, ct);

        if (beneficiary is null || request.BeneficiaryId != beneficiary.Id)
            return Result<RequestDetailDto>.Failure("غير مصرح لك بعرض هذا الطلب");

        var qr = request.QRCodeToken is null ? null : new QrCodeDto(
            request.QRCodeToken.Id,
            request.QRCodeToken.ExpiresAt,
            request.QRCodeToken.IsUsed,
            request.QRCodeToken.ExpiresAt < DateTime.UtcNow
        );

        var consultation = request.Consultation is null ? null : new ConsultationDto(
            request.Consultation.Id,
            request.Consultation.Doctor?.User?.FullNameAr ?? "—",
            request.Consultation.DiagnosisAr,
            request.Consultation.NotesAr,
            request.Consultation.RecommendationsAr,
            request.Consultation.ConsultedAt
        );

        var docs = request.Documents?.Select(d => new DocumentDto(
            d.Id,
            d.FileName,
            GetDocTypeAr(d.DocumentType),
            d.FileSizeBytes,
            d.CreatedAt
        )).ToList() ?? [];

        var dto = new RequestDetailDto(
            request.Id,
            request.Specialty?.NameAr ?? "—",
            request.Specialty?.NameEn ?? "—",
            request.Status,
            GetStatusAr(request.Status),
            GetStatusColor(request.Status),
            request.DescriptionAr,
            request.SubmittedAt,
            request.ReviewedAt,
            request.ReviewNoteAr,
            docs,
            qr,
            consultation
        );

        return Result<RequestDetailDto>.Success(dto);
    }

    private static string GetStatusAr(RequestStatus s) => s switch
    {
        RequestStatus.Draft => "مسودة",
        RequestStatus.Submitted => "تم التقديم",
        RequestStatus.UnderReview => "قيد المراجعة",
        RequestStatus.Approved => "تمت الموافقة ✓",
        RequestStatus.Rejected => "مرفوض",
        RequestStatus.Completed => "مكتمل ✓",
        _ => "—"
    };

    private static string GetStatusColor(RequestStatus s) => s switch
    {
        RequestStatus.Draft => "gray",
        RequestStatus.Submitted => "blue",
        RequestStatus.UnderReview => "orange",
        RequestStatus.Approved => "green",
        RequestStatus.Rejected => "red",
        RequestStatus.Completed => "teal",
        _ => "gray"
    };

    private static string GetDocTypeAr(DocumentType t) => t switch
    {
        DocumentType.NationalId => "بطاقة الهوية",
        DocumentType.MedicalReport => "تقرير طبي",
        DocumentType.IncomeProof => "إثبات الدخل",
        DocumentType.Other => "مستند آخر",
        _ => "—"
    };
}
