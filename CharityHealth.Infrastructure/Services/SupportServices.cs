using CharityHealth.Application.Interfaces.Services;
using CharityHealth.Domain.Entities;
using CharityHealth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CharityHealth.Infrastructure.Services;

// ─────────────────────────────────────────────────────
// Audit Service
// ─────────────────────────────────────────────────────
public class AuditService(
    AppDbContext context,
    ICurrentUserService currentUser) : IAuditService
{
    public async Task LogAsync(
        string action,
        string entityType,
        string? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        string? errorMsg = null,
        CancellationToken ct = default)
    {
        var log = new AuditLog
        {
            UserId = currentUser.UserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues ?? errorMsg,
            IpAddress = currentUser.IpAddress,
            CorrelationId = Guid.NewGuid(),
        };

        context.AuditLogs.Add(log);
        await context.SaveChangesAsync(ct);
    }

    public async Task LogAsync(string action, string entityType, string? entityId = null, string? oldValues = null, string? newValues = null, CancellationToken ct = default)
    {
         var log = new AuditLog
        {
            UserId = currentUser.UserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues ,
            IpAddress = currentUser.IpAddress,
            CorrelationId = Guid.NewGuid(),
        };

        context.AuditLogs.Add(log);
        await context.SaveChangesAsync(ct);
    }
}

// ─────────────────────────────────────────────────────
// Current User (from HttpContext)
// ─────────────────────────────────────────────────────
public class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    public string? UserId =>
        accessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    public string? UserName =>
        accessor.HttpContext?.User?.Identity?.Name;

    public string? IpAddress =>
        accessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    public bool IsAuthenticated =>
        accessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}

// ─────────────────────────────────────────────────────
// SMS Sender — Console stub (replace with real provider)
// ─────────────────────────────────────────────────────
public class ConsoleSmsStub(ILogger<ConsoleSmsStub> logger) : ISmsSender
{
    public Task SendAsync(string toPhone, string message, CancellationToken ct = default)
    {
        // TODO: Replace with real SMS gateway (e.g. Unifonic, Twilio, WhatsApp Business API)
        logger.LogInformation("[SMS STUB] To: {Phone} | Message: {Message}", toPhone, message);
        Console.WriteLine($"[SMS] To: {toPhone} => {message}");
        return Task.CompletedTask;
    }
}
