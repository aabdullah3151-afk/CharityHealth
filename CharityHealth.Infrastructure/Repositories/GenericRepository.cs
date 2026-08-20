using CharityHealth.Domain.Common;
using CharityHealth.Domain.Entities;
using CharityHealth.Domain.Interfaces.Repositories;
using CharityHealth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;

namespace CharityHealth.Infrastructure.Repositories;

// ─────────────────────────────────────────────────────
// Generic Repository
// ─────────────────────────────────────────────────────
public class GenericRepository<T>(AppDbContext context) : IGenericRepository<T>
    where T : BaseEntity
{
    protected readonly DbSet<T> _set = context.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _set.FindAsync([id], ct);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => await _set.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _set.AsNoTracking().Where(predicate).ToListAsync(ct);

    public async Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _set.AsNoTracking().FirstOrDefaultAsync(predicate, ct);

    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await _set.AddAsync(entity, ct);

    public void Update(T entity) => _set.Update(entity);

    public void Remove(T entity)
    {
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        _set.Update(entity);
    }

    public async Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _set.AnyAsync(predicate, ct);

    public async Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        => predicate is null
            ? await _set.CountAsync(ct)
            : await _set.CountAsync(predicate, ct);
}

// ─────────────────────────────────────────────────────
// Unit of Work
// ─────────────────────────────────────────────────────
public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    // ✅ كل repository يتعمل مرة واحدة بس (??= caching)
    private IBeneficiaryRepository? _beneficiaries;
    private IDoctorRepository? _doctors;
    private IGenericRepository<MedicalRequest>? _medicalRequests;
    private IGenericRepository<RequestDocument>? _requestDocuments;
    private IGenericRepository<QRCodeToken>? _qrCodeTokens;
    private IGenericRepository<Consultation>? _consultations;
    private IGenericRepository<Specialty>? _specialties;
    private IGenericRepository<OtpRecord>? _otpRecords;

    public IBeneficiaryRepository Beneficiaries
        => _beneficiaries ??= new BeneficiaryRepository(context);

    public IDoctorRepository Doctors
        => _doctors ??= new DoctorRepository(context);

    public IGenericRepository<MedicalRequest> MedicalRequests
        => _medicalRequests ??= new GenericRepository<MedicalRequest>(context);

    public IGenericRepository<RequestDocument> RequestDocuments
        => _requestDocuments ??= new GenericRepository<RequestDocument>(context);

    public IGenericRepository<QRCodeToken> QRCodeTokens
        => _qrCodeTokens ??= new GenericRepository<QRCodeToken>(context);

    public IGenericRepository<Consultation> Consultations
        => _consultations ??= new GenericRepository<Consultation>(context);

    public IGenericRepository<Specialty> Specialties
        => _specialties ??= new GenericRepository<Specialty>(context);

    public IGenericRepository<OtpRecord> OtpRecords
        => _otpRecords ??= new GenericRepository<OtpRecord>(context);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _transaction = await context.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            return;

        try
        {
            await _transaction.CommitAsync(ct);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            return;

        try
        {
            await _transaction.RollbackAsync(ct);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _transaction = null;
        // ✅ لا تعمل context.Dispose() — الـ DI هو المسؤول عن تنظيفه
    }
}
