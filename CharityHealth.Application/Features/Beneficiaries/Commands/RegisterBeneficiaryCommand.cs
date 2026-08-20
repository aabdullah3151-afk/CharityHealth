using CharityHealth.Application.Interfaces.Services;
using CharityHealth.Domain.Entities;
using CharityHealth.Domain.Enums;
using CharityHealth.Domain.Interfaces.Repositories;
using CharityHealth.Shared.Wrappers;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CharityHealth.Application.Features.Beneficiaries.Commands;

// ── Command ───────────────────────────────────────────
public record RegisterBeneficiaryCommand(
    string FullNameAr,
    string FullNameEn,
    string PhoneNumber,
    string? Email,
    string Password,
    string NationalId,
    DateOnly DateOfBirth,
    Gender Gender,
    string? City,
    string? AddressAr
) : IRequest<Result<RegisterBeneficiaryResult>>;

public record RegisterBeneficiaryResult(string UserId, string FullNameAr);

// ── Validator ─────────────────────────────────────────
public class RegisterBeneficiaryValidator : AbstractValidator<RegisterBeneficiaryCommand>
{
    public RegisterBeneficiaryValidator()
    {
        RuleFor(x => x.FullNameAr)
            .NotEmpty().WithMessage("الاسم الكامل بالعربية مطلوب")
            .MaximumLength(200);
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("رقم الهاتف مطلوب")
            .Matches(@"^01[0125][0-9]{8}$")
            .WithMessage("رقم الهاتف يجب أن يكون 11 رقمًا ويبدأ بـ 010 أو 011 أو 012 أو 015");

RuleFor(x => x.Password)
     .NotEmpty().WithMessage("كلمة المرور مطلوبة")
     .MinimumLength(6).WithMessage("كلمة المرور 6 أحرف على الأقل");
        RuleFor(x => x.NationalId)
            .NotEmpty().WithMessage("الرقم القومي مطلوب")
            .Matches(@"^[0-9]{14}$")
            .WithMessage("الرقم القومي يجب أن يتكون من 14 رقمًا فقط");

RuleFor(x => x.DateOfBirth)
            .LessThan(DateOnly.FromDateTime(DateTime.Today.AddYears(-10)))
            .WithMessage("العمر يجب أن يكون أكثر من 10 سنوات");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("بريد إلكتروني غير صحيح")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}

// ── Handler ───────────────────────────────────────────
public class RegisterBeneficiaryHandler(
    UserManager<ApplicationUser> userManager,
    IUnitOfWork uow,
    IAuditService audit,
    ILogger<RegisterBeneficiaryHandler> logger)
    : IRequestHandler<RegisterBeneficiaryCommand, Result<RegisterBeneficiaryResult>>
{
    public async Task<Result<RegisterBeneficiaryResult>> Handle(
        RegisterBeneficiaryCommand cmd, CancellationToken ct)
    {
        // Check duplicate NationalId
        if (await uow.Beneficiaries.ExistsAsync(b => b.NationalId == cmd.NationalId, ct))
            return Result<RegisterBeneficiaryResult>.Failure("رقم الهوية مسجل مسبقاً في النظام");

        // Check duplicate phone
        var existingPhone = userManager.Users.FirstOrDefault(u => u.PhoneNumber == cmd.PhoneNumber);
        if (existingPhone is not null)
            return Result<RegisterBeneficiaryResult>.Failure("رقم الهاتف مسجل مسبقاً");

        await uow.BeginTransactionAsync(ct);
        try
        {
            // 1. Create Identity User
            var user = new ApplicationUser
            {
                UserName = cmd.PhoneNumber,
                PhoneNumber = cmd.PhoneNumber,
                Email = cmd.Email,
                FullNameAr = cmd.FullNameAr,
                FullNameEn = cmd.FullNameEn,
                UserType = UserType.Beneficiary,
                IsActive = true,
                PhoneNumberConfirmed = true,
                EmailConfirmed = string.IsNullOrEmpty(cmd.Email) ? false : false,
            };

            var identityResult = await userManager.CreateAsync(user, cmd.Password);
            if (!identityResult.Succeeded)
            {
                await uow.RollbackTransactionAsync(ct);
                var errors = identityResult.Errors.Select(e => e.Description).ToList();
                return Result<RegisterBeneficiaryResult>.Failure(errors);
            }

            var roleResult = await userManager.AddToRoleAsync(user, "Beneficiary");
            if (!roleResult.Succeeded)
            {
                await uow.RollbackTransactionAsync(ct);
                var roleErrors = roleResult.Errors.Select(e => e.Description).ToList();
                return Result<RegisterBeneficiaryResult>.Failure(roleErrors);
            }

            // 2. Create Beneficiary profile
            var beneficiary = new Beneficiary
            {
                UserId = user.Id,
                NationalId = cmd.NationalId,
                DateOfBirth = cmd.DateOfBirth,
                Gender = cmd.Gender,
                City = cmd.City,
                AddressAr = cmd.AddressAr,
            };

            await uow.Beneficiaries.AddAsync(beneficiary, ct);
            await uow.SaveChangesAsync(ct);
            await uow.CommitTransactionAsync(ct);

            // التسجيل تم بالفعل بعد الـ Commit، لذلك فشل الـ Audit لا يجب أن
            // يرجع للمستخدم رسالة أن إنشاء الحساب فشل.
            try
            {
                await audit.LogAsync("Beneficiary.Registered", "ApplicationUser", user.Id,
                    newValues: $"{{\"phone\":\"{cmd.PhoneNumber}\",\"nationalId\":\"{cmd.NationalId}\"}}");
            }
            catch (Exception auditEx)
            {
                logger.LogWarning(auditEx,
                    "Beneficiary {UserId} registered successfully but audit logging failed",
                    user.Id);
            }

            logger.LogInformation("New beneficiary registered: {UserId}", user.Id);

            return Result<RegisterBeneficiaryResult>.Success(
                new RegisterBeneficiaryResult(user.Id, user.FullNameAr),
                "تم التسجيل بنجاح!");
        }
        catch (Exception ex)
        {
            try
            {
                await uow.RollbackTransactionAsync(ct);
            }
            catch (Exception rollbackEx)
            {
                logger.LogWarning(rollbackEx, "Registration transaction rollback failed");
            }

            logger.LogError(ex, "Registration failed");
            return Result<RegisterBeneficiaryResult>.Failure("حدث خطأ أثناء التسجيل. يرجى المحاولة لاحقاً.");
        }
    }
}
