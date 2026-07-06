using CharityHealth.Domain.Entities;

namespace CharityHealth.Domain.Interfaces.Repositories;

public interface IBeneficiaryRepository : IGenericRepository<Beneficiary>
{
    /// <summary>مستفيد مع بيانات المستخدم</summary>
    Task<Beneficiary?> GetByIdWithUserAsync(Guid id, CancellationToken ct = default);

    /// <summary>مستفيد عن طريق UserId</summary>
    Task<Beneficiary?> GetByUserIdAsync(string userId, CancellationToken ct = default);

    /// <summary>مستفيد عن طريق رقم الهوية</summary>
    Task<Beneficiary?> GetByNationalIdAsync(string nationalId, CancellationToken ct = default);

    /// <summary>قائمة المستفيدين مع بيانات المستخدم</summary>
    Task<IReadOnlyList<Beneficiary>> GetAllWithUserAsync(CancellationToken ct = default);

    /// <summary>البحث بالاسم أو رقم الهوية أو الهاتف</summary>
    Task<IReadOnlyList<Beneficiary>> SearchAsync(string keyword, CancellationToken ct = default);

    /// <summary>مستفيد مع كل طلباته</summary>
    Task<Beneficiary?> GetWithRequestsAsync(Guid id, CancellationToken ct = default);

    /// <summary>هل رقم الهوية مسجل مسبقاً؟</summary>
    Task<bool> NationalIdExistsAsync(string nationalId, CancellationToken ct = default);

    /// <summary>إجمالي عدد المستفيدين النشطين</summary>
    Task<int> GetActiveCountAsync(CancellationToken ct = default);
}
