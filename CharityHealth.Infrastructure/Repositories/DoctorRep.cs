using CharityHealth.Domain.Entities;
using CharityHealth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using CharityHealth.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace CharityHealth.Infrastructure.Repositories
{

    public class DoctorRepository(AppDbContext context)
        : GenericRepository<Doctor>(context), IDoctorRepository
    {
        private readonly AppDbContext _context = context;

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Doctor>> GetAllWithDetailsAsync(
            CancellationToken ct = default)
            => await _context.Doctors
                .AsNoTracking()
                .Include(d => d.User)
                .Include(d => d.Specialty)
                .OrderBy(d => d.Specialty.NameAr)
                .ThenBy(d => d.User.FullNameAr)
                .ToListAsync(ct);

        /// <inheritdoc/>
        public async Task<Doctor?> GetByIdWithDetailsAsync(
            Guid id, CancellationToken ct = default)
            => await _context.Doctors
                .AsNoTracking()
                .Include(d => d.User)
                .Include(d => d.Specialty)
                .Include(d => d.Consultations)
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, ct);

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Doctor>> GetBySpecialtyAsync(
            Guid specialtyId, CancellationToken ct = default)
            => await _context.Doctors
                .AsNoTracking()
                .Include(d => d.User)
                .Where(d => d.SpecialtyId == specialtyId
                         && d.IsAvailable
                         && d.User.IsActive
                         && !d.IsDeleted)
                .OrderBy(d => d.User.FullNameAr)
                .ToListAsync(ct);

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Doctor>> GetAvailableTodayAsync(
            CancellationToken ct = default)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // جلب الأطباء المتاحين مع عدد كشوفاتهم اليوم
            var doctors = await _context.Doctors
                .AsNoTracking()
                .Include(d => d.User)
                .Include(d => d.Specialty)
                .Include(d => d.Consultations
                    .Where(c => DateOnly.FromDateTime(c.ConsultedAt) == today))
                .Where(d => d.IsAvailable && d.User.IsActive && !d.IsDeleted)
                .ToListAsync(ct);

            // فلتر من لم يصل للحد الأقصى اليوم
            return doctors
                .Where(d => d.Consultations.Count < d.MaxDailySlots)
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Doctor>> SearchAsync(
            string keyword, CancellationToken ct = default)
        {
            var kw = keyword.Trim().ToLower();

            return await _context.Doctors
                .AsNoTracking()
                .Include(d => d.User)
                .Include(d => d.Specialty)
                .Where(d => !d.IsDeleted && (
                    d.User.FullNameAr.ToLower().Contains(kw) ||
                    d.User.FullNameEn.ToLower().Contains(kw) ||
                    d.LicenseNumber.ToLower().Contains(kw) ||
                    d.User.PhoneNumber!.Contains(kw) ||
                    d.Specialty.NameAr.ToLower().Contains(kw)))
                .OrderBy(d => d.User.FullNameAr)
                .ToListAsync(ct);
        }

        /// <inheritdoc/>
        public async Task<int> GetConsultationCountAsync(
            Guid doctorId, DateOnly date, CancellationToken ct = default)
            => await _context.Consultations
                .Where(c => c.Doctor.Id == doctorId
                         && DateOnly.FromDateTime(c.ConsultedAt) == date)
                .CountAsync(ct);

        /// <inheritdoc/>
        public async Task<bool> HasReachedDailyLimitAsync(
            Guid doctorId, CancellationToken ct = default)
        {
            var doctor = await _context.Doctors
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == doctorId, ct);

            if (doctor is null) return true;

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var count = await GetConsultationCountAsync(doctorId, today, ct);

            return count >= doctor.MaxDailySlots;
        }

        /// <inheritdoc/>
        public async Task<Doctor?> GetByUserIdAsync(
            string userId, CancellationToken ct = default)
            => await _context.Doctors
                .AsNoTracking()
                .Include(d => d.User)
                .Include(d => d.Specialty)
                .FirstOrDefaultAsync(d => d.UserId == userId && !d.IsDeleted, ct);
    }
}
