using CharityHealth.Domain.Entities;
using CharityHealth.Domain.Interfaces.Repositories;
using CharityHealth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CharityHealth.Infrastructure.Repositories;

public class BeneficiaryRepository(AppDbContext context)
    : GenericRepository<Beneficiary>(context), IBeneficiaryRepository
{
    private readonly AppDbContext _context = context;

    /// <inheritdoc/>
    public async Task<Beneficiary?> GetByIdWithUserAsync(
        Guid id, CancellationToken ct = default)
        => await _context.Beneficiaries
            .AsNoTracking()
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, ct);

    /// <inheritdoc/>
    public async Task<Beneficiary?> GetByUserIdAsync(
        string userId, CancellationToken ct = default)
        => await _context.Beneficiaries
            .AsNoTracking()
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.UserId == userId && !b.IsDeleted, ct);

    /// <inheritdoc/>
    public async Task<Beneficiary?> GetByNationalIdAsync(
        string nationalId, CancellationToken ct = default)
        => await _context.Beneficiaries
            .AsNoTracking()
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.NationalId == nationalId && !b.IsDeleted, ct);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Beneficiary>> GetAllWithUserAsync(
        CancellationToken ct = default)
        => await _context.Beneficiaries
            .AsNoTracking()
            .Include(b => b.User)
            .Where(b => !b.IsDeleted)
            .OrderBy(b => b.User.FullNameAr)
            .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Beneficiary>> SearchAsync(
        string keyword, CancellationToken ct = default)
    {
        var kw = keyword.Trim().ToLower();

        return await _context.Beneficiaries
            .AsNoTracking()
            .Include(b => b.User)
            .Where(b => !b.IsDeleted && (
                b.User.FullNameAr.ToLower().Contains(kw) ||
                b.User.FullNameEn.ToLower().Contains(kw) ||
                b.NationalId.Contains(kw) ||
                b.User.PhoneNumber!.Contains(kw) ||
                b.City!.ToLower().Contains(kw)))
            .OrderBy(b => b.User.FullNameAr)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<Beneficiary?> GetWithRequestsAsync(
        Guid id, CancellationToken ct = default)
        => await _context.Beneficiaries
            .AsNoTracking()
            .Include(b => b.User)
            .Include(b => b.MedicalRequests
                .Where(r => !r.IsDeleted)
                .OrderByDescending(r => r.SubmittedAt))
                .ThenInclude(r => r.Specialty)
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, ct);

    /// <inheritdoc/>
    public async Task<bool> NationalIdExistsAsync(
        string nationalId, CancellationToken ct = default)
        => await _context.Beneficiaries
            .AnyAsync(b => b.NationalId == nationalId && !b.IsDeleted, ct);

    /// <inheritdoc/>
    public async Task<int> GetActiveCountAsync(CancellationToken ct = default)
        => await _context.Beneficiaries
            .CountAsync(b => !b.IsDeleted && b.User.IsActive, ct);
}
