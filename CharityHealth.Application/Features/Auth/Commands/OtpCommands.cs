using CharityHealth.Application.Common.Models;
using CharityHealth.Application.Interfaces.Services;
using CharityHealth.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CharityHealth.Application.Features.Auth.Commands;

// ═══════════════════════════════════════════════════════
// SEND OTP
// ═══════════════════════════════════════════════════════
public record SendOtpCommand(string PhoneNumber) : IRequest<Result>;

public class SendOtpCommandValidator : AbstractValidator<SendOtpCommand>
{
    public SendOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("رقم الهاتف مطلوب")
            .Matches(@"^\+?[0-9]{8,15}$").WithMessage("رقم الهاتف غير صحيح");
    }
}

public class SendOtpCommandHandler(
    UserManager<ApplicationUser> userManager,
    IOtpService otpService,
    ILogger<SendOtpCommandHandler> logger)
    : IRequestHandler<SendOtpCommand, Result>
{
    public async Task<Result> Handle(SendOtpCommand request, CancellationToken ct)
    {
        // Look up user by phone number
        var user = userManager.Users.FirstOrDefault(u => u.PhoneNumber == request.PhoneNumber);
        if (user is null || !user.IsActive)
        {
            // Return generic message — don't reveal if phone exists
            logger.LogWarning("OTP requested for unknown/inactive phone: {Phone}", request.PhoneNumber);
            return Result.Success("إذا كان رقم الهاتف مسجلاً، ستصلك رسالة OTP خلال لحظات");
        }

        var sent = await otpService.SendOtpAsync(user.Id, request.PhoneNumber, ct);

        if (!sent)
        {
            return Result.Failure("تعذر إرسال رمز التحقق. يرجى المحاولة لاحقاً.");
        }

        logger.LogInformation("OTP sent to user {UserId}", user.Id);
        return Result.Success("تم إرسال رمز التحقق إلى هاتفك");
    }
}

// ═══════════════════════════════════════════════════════
// VERIFY OTP (Login)
// ═══════════════════════════════════════════════════════
public record VerifyOtpCommand(string PhoneNumber, string OtpCode) : IRequest<AuthResult>;

public class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("رقم الهاتف مطلوب");

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("رمز التحقق مطلوب")
            .Length(6).WithMessage("رمز التحقق يجب أن يكون 6 أرقام")
            .Matches(@"^\d{6}$").WithMessage("رمز التحقق يجب أن يحتوي على أرقام فقط");
    }
}

public class VerifyOtpCommandHandler(
    UserManager<ApplicationUser> userManager,
    IOtpService otpService,
    IAuditService auditService,
    ILogger<VerifyOtpCommandHandler> logger)
    : IRequestHandler<VerifyOtpCommand, AuthResult>
{
    public async Task<AuthResult> Handle(VerifyOtpCommand request, CancellationToken ct)
    {
        var verifyResult = await otpService.VerifyOtpAsync(request.PhoneNumber, request.OtpCode, ct);

        if (!verifyResult.Success)
        {
            if (verifyResult.IsLocked)
                return new AuthResult(false, null, null, null, null,
                    "تم تجاوز عدد المحاولات المسموح بها. يرجى المحاولة بعد 15 دقيقة.");

            return new AuthResult(false, null, null, null, null, verifyResult.ErrorMessage);
        }

        var user = userManager.Users.FirstOrDefault(u => u.PhoneNumber == request.PhoneNumber);
        if (user is null || !user.IsActive)
            return new AuthResult(false, null, null, null, null, "المستخدم غير موجود أو موقف");

        var roles = await userManager.GetRolesAsync(user);

        await auditService.LogAsync("Auth.Login.Success", "ApplicationUser", user.Id,
            newValues: "{\"method\":\"otp\"}");

        logger.LogInformation("User {UserId} logged in via OTP", user.Id);

        return new AuthResult(true, user.Id, user.FullNameAr, user.UserType.ToString(),
            [.. roles], null);
    }
}

// Re-usable Result (simple non-generic)
public class Result
{
    public bool Succeeded { get; private set; }
    public string? Message { get; private set; }
    public List<string> Errors { get; private set; } = [];

    public static Result Success(string? message = null) => new() { Succeeded = true, Message = message };
    public static Result Failure(string error) => new() { Succeeded = false, Errors = [error] };
}
