using CharityHealth.Application.Common.Models;
using CharityHealth.Application.Interfaces.Services;
using CharityHealth.Domain.Entities;
using CharityHealth.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CharityHealth.Application.Features.Auth.Commands;

// ── Command ───────────────────────────────────────────
public record LoginCommand(string UserNameOrEmail, string Password) : IRequest<AuthResult>;

// ── Validator ─────────────────────────────────────────
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.UserNameOrEmail)
            .NotEmpty().WithMessage("اسم المستخدم أو البريد الإلكتروني مطلوب");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("كلمة المرور مطلوبة")
            .MinimumLength(8).WithMessage("كلمة المرور يجب أن تكون 8 أحرف على الأقل");
    }
}

// ── Handler ───────────────────────────────────────────
public class LoginCommandHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IAuditService auditService,
    ILogger<LoginCommandHandler> logger)
    : IRequestHandler<LoginCommand, AuthResult>
{
    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken ct)
    {
        // Find user by email or username
        var user = await userManager.FindByEmailAsync(request.UserNameOrEmail)
                   ?? await userManager.FindByNameAsync(request.UserNameOrEmail);

        if (user is null || !user.IsActive)
        {
            logger.LogWarning("Login failed — user not found: {Input}", request.UserNameOrEmail);
            await auditService.LogAsync("Auth.Login.Failed", "ApplicationUser",
                errorMsg: "User not found or inactive");
            return new AuthResult(false, null, null, null, null, "اسم المستخدم أو كلمة المرور غير صحيحة");
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            logger.LogWarning("Account locked: {UserId}", user.Id);
            return new AuthResult(false, null, null, null, null,
                "الحساب مقفل مؤقتاً بسبب تكرار محاولات الدخول الخاطئة. حاول بعد 15 دقيقة.");
        }

        if (!result.Succeeded)
        {
            await auditService.LogAsync("Auth.Login.Failed", "ApplicationUser", user.Id,
                errorMsg: "Invalid password");
            return new AuthResult(false, null, null, null, null, "اسم المستخدم أو كلمة المرور غير صحيحة");
        }

        var roles = await userManager.GetRolesAsync(user);

        await auditService.LogAsync("Auth.Login.Success", "ApplicationUser", user.Id,
            newValues: $"{{\"method\":\"password\",\"roles\":\"{string.Join(",", roles)}\"}}");

        logger.LogInformation("User {UserId} logged in via password", user.Id);

        return new AuthResult(true, user.Id, user.FullNameAr, user.UserType.ToString(),
            [.. roles], null);
    }
}

// needed for validator — pull FluentValidation
public abstract class AbstractValidator<T> : FluentValidation.AbstractValidator<T> { }
public static class RuleBuilderExtensions
{
    // convenience re-export
}
