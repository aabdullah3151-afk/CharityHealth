using System.Linq.Expressions;
using CharityHealth.Domain.Common;
using CharityHealth.Domain.Interfaces.Repositories;
using CharityHealth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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

    public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _set.AsNoTracking().Where(predicate).ToListAsync(ct);

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _set.AsNoTracking().FirstOrDefaultAsync(predicate, ct);

    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await _set.AddAsync(entity, ct);

    public void Update(T entity)
        => _set.Update(entity);

    public void Remove(T entity)
    {
        entity.IsDeleted = true;         // Soft delete
        entity.UpdatedAt = DateTime.UtcNow;
        _set.Update(entity);
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _set.AnyAsync(predicate, ct);

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
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

    public IGenericRepository<Domain.Entities.Beneficiary> Beneficiaries
        => new GenericRepository<Domain.Entities.Beneficiary>(context);
    public IGenericRepository<Domain.Entities.Doctor> Doctors
        => new GenericRepository<Domain.Entities.Doctor>(context);
    public IGenericRepository<Domain.Entities.MedicalRequest> MedicalRequests
        => new GenericRepository<Domain.Entities.MedicalRequest>(context);
    public IGenericRepository<Domain.Entities.RequestDocument> RequestDocuments
        => new GenericRepository<Domain.Entities.RequestDocument>(context);
    public IGenericRepository<Domain.Entities.QRCodeToken> QRCodeTokens
        => new GenericRepository<Domain.Entities.QRCodeToken>(context);
    public IGenericRepository<Domain.Entities.Consultation> Consultations
        => new GenericRepository<Domain.Entities.Consultation>(context);
    public IGenericRepository<Domain.Entities.Specialty> Specialties
        => new GenericRepository<Domain.Entities.Specialty>(context);
    public IGenericRepository<Domain.Entities.OtpRecord> OtpRecords
        => new GenericRepository<Domain.Entities.OtpRecord>(context);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _transaction = await context.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
            await _transaction.CommitAsync(ct);
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
            await _transaction.RollbackAsync(ct);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        context.Dispose();
    }
}
