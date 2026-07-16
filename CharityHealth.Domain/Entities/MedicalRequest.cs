using CharityHealth.Domain.Common;
using CharityHealth.Domain.Enums;

namespace CharityHealth.Domain.Entities;

public class MedicalRequest : BaseEntity
{
    public Guid BeneficiaryId { get; set; }
    public Guid? SpecialtyId { get; set; }
    public Guid? DoctorId { get; set; }
    public DateOnly? AppointmentDate { get; set; }
    public ServiceRequestType ServiceType { get; set; } = ServiceRequestType.MedicalConsultation;
    public string? AssignedProviderUserId { get; set; }
    public string? ProviderNoteAr { get; set; }
    public DateTime? FulfilledAt { get; set; }
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
    public Specialty? Specialty { get; set; }
    public Doctor? Doctor { get; set; }
    public ApplicationUser? AssignedProvider { get; set; }
    public ICollection<RequestDocument> Documents { get; set; } = [];
    public QRCodeToken? QRCodeToken { get; set; }
    public Consultation? Consultation { get; set; }
}
