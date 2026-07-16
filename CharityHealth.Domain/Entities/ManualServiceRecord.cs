using CharityHealth.Domain.Common;
using CharityHealth.Domain.Enums;

namespace CharityHealth.Domain.Entities;

public class ManualServiceRecord : BaseEntity
{
    public ServiceRequestType ServiceType { get; set; }

    public string ProviderUserId { get; set; } = string.Empty;

    public Guid? DoctorId { get; set; }

    public DateOnly ServiceDate { get; set; }

    public int Quantity { get; set; } = 1;

    public string? Notes { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;
}
