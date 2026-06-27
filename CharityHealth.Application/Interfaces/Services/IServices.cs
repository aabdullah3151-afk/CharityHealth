namespace CharityHealth.Application.Interfaces.Services;

// ─── OTP ───────────────────────────────────────────────
public interface IOtpService
{
    /// <summary>Generate OTP, hash it, save to DB, send via SMS provider.</summary>
    Task<bool> SendOtpAsync(string userId, string phoneNumber, CancellationToken ct = default);

    /// <summary>Verify OTP. Increments failed attempts. Locks after 3 failures.</summary>
    Task<OtpVerifyResult> VerifyOtpAsync(string phoneNumber, string otpCode, CancellationToken ct = default);
}

public record OtpVerifyResult(bool Success, string? ErrorMessage, bool IsLocked = false);

// ─── SMS / Messaging (abstracted provider) ─────────────
public interface ISmsSender
{
    Task SendAsync(string toPhone, string message, CancellationToken ct = default);
}

// ─── Audit ─────────────────────────────────────────────
public interface IAuditService
{
    Task LogAsync(string action, string entityType, string? entityId = null,
        string? oldValues = null, string? newValues = null, string? errorMsg = null, CancellationToken ct = default);
}

// ─── QR Code ───────────────────────────────────────────
public interface IQRCodeService
{
    /// <summary>Signs payload, returns (rawToken, tokenHash, qrImageBase64)</summary>
    Task<QRGenerateResult> GenerateAsync(Guid requestId, Guid beneficiaryId, CancellationToken ct = default);

    /// <summary>Verifies signature, expiry, and usage of a scanned token.</summary>
    Task<QRVerifyResult> VerifyAsync(string rawToken, CancellationToken ct = default);
}

public record QRGenerateResult(string RawToken, string TokenHash, string QrImageBase64, DateTime ExpiresAt);
public record QRVerifyResult(bool Valid, Guid? RequestId, Guid? BeneficiaryId, string? ErrorMessage);

// ─── File Storage ──────────────────────────────────────
public interface IFileStorageService
{
    Task<string> SaveAsync(Stream fileStream, string fileName, string folder, CancellationToken ct = default);
    Task DeleteAsync(string filePath, CancellationToken ct = default);
    string GetPublicUrl(string filePath);
}

// ─── Current User ──────────────────────────────────────
public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    string? IpAddress { get; }
    bool IsAuthenticated { get; }
}

public interface INotificationSender
{
    Task SendToUserAsync(string userId, string eventName, object payload, CancellationToken ct = default);
    Task SendToRoleGroupAsync(string role, string eventName, object payload, CancellationToken ct = default);
}
