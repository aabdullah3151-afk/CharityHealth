using CharityHealth.Domain.Entities;

namespace CharityHealth.Domain.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    IBeneficiaryRepository Beneficiaries { get; }
    IDoctorRepository Doctors { get; }
    IGenericRepository<MedicalRequest> MedicalRequests { get; }
    IGenericRepository<RequestDocument> RequestDocuments { get; }
    IGenericRepository<QRCodeToken> QRCodeTokens { get; }
    IGenericRepository<Consultation> Consultations { get; }
    IGenericRepository<Specialty> Specialties { get; }
    IGenericRepository<OtpRecord> OtpRecords { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
