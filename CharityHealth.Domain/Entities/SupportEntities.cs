using CharityHealth.Domain.Common;
using CharityHealth.Domain.Enums;

namespace CharityHealth.Domain.Entities;

// ─────────────────────────────────────────────
public class RequestDocument : BaseEntity
{
    public Guid RequestId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DocumentType DocumentType { get; set; }

    public MedicalRequest Request { get; set; } = null!;
}

// ─────────────────────────────────────────────
/// <summary>
/// Secure single-use QR token. Raw token is NEVER stored — only its SHA-256 hash.
/// </summary>
public class QRCodeToken : BaseEntity
{
    public Guid RequestId { get; set; }

    /// <summary>SHA-256 hash of the signed JWT-like token. Never store raw token.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime? UsedAt { get; set; }
    public string? UsedByDoctorId { get; set; }

    // Navigation
    public MedicalRequest Request { get; set; } = null!;
    public Consultation? Consultation { get; set; }
}

// ─────────────────────────────────────────────
public class Consultation : BaseEntity
{
    public Guid RequestId { get; set; }
    public Guid QRCodeTokenId { get; set; }
    public string DoctorId { get; set; } = string.Empty;  // ApplicationUser.Id

    public string DiagnosisAr { get; set; } = string.Empty;
    public string DiagnosisEn { get; set; } = string.Empty;
    public string? NotesAr { get; set; }
    public string? NotesEn { get; set; }
    public string? RecommendationsAr { get; set; }
    public string? RecommendationsEn { get; set; }
    public DateTime ConsultedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public MedicalRequest Request { get; set; } = null!;
    public QRCodeToken QRCodeToken { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
}

// ─────────────────────────────────────────────
public class OtpRecord : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>bcrypt hash of the OTP — raw OTP is never stored.</summary>
    public string CodeHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public int FailedAttempts { get; set; } = 0;
    public bool IsLocked { get; set; } = false;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}

// ─────────────────────────────────────────────
/// <summary>Immutable audit trail — no UPDATE or DELETE ever performed on this table.</summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? UserId { get; set; }
    public string Action { get; set; } = string.Empty;       // e.g. "Request.Approved"
    public string EntityType { get; set; } = string.Empty;   // e.g. "MedicalRequest"
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }   // JSON
    public string? NewValues { get; set; }   // JSON
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Guid? CorrelationId { get; set; }
}

// ─────────────────────────────────────────────
public class LoginHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public DateTime LoginAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public LoginMethod LoginMethod { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
