using CharityHealth.Application.Interfaces.Services;
using CharityHealth.Domain.Entities;
using CharityHealth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BC = BCrypt.Net.BCrypt;

namespace CharityHealth.Infrastructure.Services;

public class OtpService(
    AppDbContext context,
    ISmsSender smsSender,
    ILogger<OtpService> logger) : IOtpService
{
    private const int OtpLength = 6;
    private const int ExpiryMinutes = 5;
    private const int MaxAttempts = 3;
    private const int LockMinutes = 15;

    public async Task<bool> SendOtpAsync(string userId, string phoneNumber, CancellationToken ct = default)
    {
        try
        {
            // Invalidate any existing unused OTPs for this phone
            var existing = await context.OtpRecords
                .Where(o => o.PhoneNumber == phoneNumber && !o.IsUsed)
                .ToListAsync(ct);

            foreach (var old in existing)
                old.IsUsed = true;

            // Generate 6-digit OTP
            var rawOtp = GenerateOtp();
            var hash = BC.HashPassword(rawOtp);

            var record = new OtpRecord
            {
                UserId = userId,
                PhoneNumber = phoneNumber,
                CodeHash = hash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(ExpiryMinutes),
                IsUsed = false,
                FailedAttempts = 0
            };

            context.OtpRecords.Add(record);
            await context.SaveChangesAsync(ct);

            // Send SMS
            var message = $"رمز التحقق الخاص بك هو: {rawOtp}\nصالح لمدة {ExpiryMinutes} دقائق.";
            await smsSender.SendAsync(phoneNumber, message, ct);

            logger.LogInformation("OTP sent to {Phone}", phoneNumber);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send OTP to {Phone}", phoneNumber);
            return false;
        }
    }

    public async Task<OtpVerifyResult> VerifyOtpAsync(string phoneNumber, string otpCode, CancellationToken ct = default)
    {
        var record = await context.OtpRecords
            .Where(o => o.PhoneNumber == phoneNumber && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (record is null)
            return new OtpVerifyResult(false, "رمز التحقق غير صحيح أو منتهي الصلاحية");

        if (record.IsLocked)
            return new OtpVerifyResult(false, "تم تجاوز عدد المحاولات المسموح بها", IsLocked: true);

        if (record.ExpiresAt < DateTime.UtcNow)
        {
            record.IsUsed = true;
            await context.SaveChangesAsync(ct);
            return new OtpVerifyResult(false, "انتهت صلاحية رمز التحقق. يرجى طلب رمز جديد.");
        }

        if (!BC.Verify(otpCode, record.CodeHash))
        {
            record.FailedAttempts++;

            if (record.FailedAttempts >= MaxAttempts)
            {
                record.IsLocked = true;
                logger.LogWarning("OTP locked for {Phone} after {N} failed attempts", phoneNumber, MaxAttempts);
            }

            await context.SaveChangesAsync(ct);

            var remaining = MaxAttempts - record.FailedAttempts;
            return remaining <= 0
                ? new OtpVerifyResult(false, "تم تجاوز عدد المحاولات. سيُفتح الحساب بعد 15 دقيقة.", IsLocked: true)
                : new OtpVerifyResult(false, $"رمز التحقق غير صحيح. المحاولات المتبقية: {remaining}");
        }

        // ✅ Valid OTP
        record.IsUsed = true;
        await context.SaveChangesAsync(ct);

        logger.LogInformation("OTP verified for {Phone}", phoneNumber);
        return new OtpVerifyResult(true, null);
    }

    private static string GenerateOtp()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }
}
