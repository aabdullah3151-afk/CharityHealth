using CharityHealth.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CharityHealth.Domain.Interfaces.Repositories
{

    public interface IDoctorRepository : IGenericRepository<Doctor>
    {
        /// <summary>الأطباء مع بيانات المستخدم والتخصص</summary>
        Task<IReadOnlyList<Doctor>> GetAllWithDetailsAsync(CancellationToken ct = default);

        /// <summary>دكتور واحد مع كل تفاصيله</summary>
        Task<Doctor?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

        /// <summary>أطباء تخصص معين</summary>
        Task<IReadOnlyList<Doctor>> GetBySpecialtyAsync(Guid specialtyId, CancellationToken ct = default);

        /// <summary>الأطباء المتاحين للكشف اليوم</summary>
        Task<IReadOnlyList<Doctor>> GetAvailableTodayAsync(CancellationToken ct = default);

        /// <summary>البحث بالاسم أو رقم الرخصة</summary>
        Task<IReadOnlyList<Doctor>> SearchAsync(string keyword, CancellationToken ct = default);

        /// <summary>عدد كشوفات الدكتور في يوم معين</summary>
        Task<int> GetConsultationCountAsync(Guid doctorId, DateOnly date, CancellationToken ct = default);

        /// <summary>هل وصل الدكتور للحد الأقصى اليوم؟</summary>
        Task<bool> HasReachedDailyLimitAsync(Guid doctorId, CancellationToken ct = default);

        /// <summary>دكتور بواسطة UserId</summary>
        Task<Doctor?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    }
}
