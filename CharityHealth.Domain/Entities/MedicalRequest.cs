using CharityHealth.Domain.Common;
using CharityHealth.Domain.Enums;

namespace CharityHealth.Domain.Entities;

public class MedicalRequest : BaseEntity
{
    public Guid BeneficiaryId { get; set; }
    public Guid SpecialtyId { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Draft;

    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }

    public DateTime SubmittedAt { get; set; }
    public string? ReviewedBy { get; set; }      // UserId of Staff
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNoteAr { get; set; }
    public string? ReviewNoteEn { get; set; }

    // Navigation
    public Beneficiary Beneficiary { get; set; } = null!;
    public Specialty Specialty { get; set; } = null!;
    public ICollection<RequestDocument> Documents { get; set; } = [];
    public QRCodeToken? QRCodeToken { get; set; }
    public Consultation? Consultation { get; set; }
}
