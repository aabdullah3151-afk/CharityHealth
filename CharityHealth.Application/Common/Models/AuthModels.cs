namespace CharityHealth.Application.Common.Models;

public record LoginRequest(string UserNameOrEmail, string Password);
public record SendOtpRequest(string PhoneNumber);
public record VerifyOtpRequest(string PhoneNumber, string OtpCode);

public record AuthResult(
    bool Succeeded,
    string? UserId,
    string? FullName,
    string? UserType,
    string[]? Roles,
    string? ErrorMessage
);
