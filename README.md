
# CharityHealth

# ═══════════════════════════════════════════════════
# NuGet Packages — Healthcare Charity Management System
# ═══════════════════════════════════════════════════

# ─── CharityHealth.Domain ───────────────────────────
# (No NuGet packages — pure C# only)


# ─── CharityHealth.Application ──────────────────────
MediatR                              v12.x
FluentValidation.DependencyInjection v11.x
Microsoft.AspNetCore.Identity.Core   (via metapackage)


# ─── CharityHealth.Infrastructure ───────────────────
Microsoft.EntityFrameworkCore                     v9.x
Microsoft.EntityFrameworkCore.Design              v9.x
Npgsql.EntityFrameworkCore.PostgreSQL             v9.x
Microsoft.AspNetCore.Identity.EntityFrameworkCore v9.x
BCrypt.Net-Next                                   v4.x
QRCoder                                           v1.x


# ─── CharityHealth.Web (Blazor Server) ──────────────
Microsoft.AspNetCore.Components.Server            (included in SDK)
Microsoft.AspNetCore.SignalR                      (included in SDK)


# ─── CharityHealth.Shared ───────────────────────────
# (No NuGet packages — DTOs and constants only)


# ═══════════════════════════════════════════════════
# CLI Commands to install packages
# ═══════════════════════════════════════════════════

# -- Application project --
dotnet add src/CharityHealth.Application/CharityHealth.Application.csproj package MediatR
dotnet add src/CharityHealth.Application/CharityHealth.Application.csproj package FluentValidation.DependencyInjection

# -- Infrastructure project --
dotnet add src/CharityHealth.Infrastructure/CharityHealth.Infrastructure.csproj package Microsoft.EntityFrameworkCore
dotnet add src/CharityHealth.Infrastructure/CharityHealth.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design
dotnet add src/CharityHealth.Infrastructure/CharityHealth.Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/CharityHealth.Infrastructure/CharityHealth.Infrastructure.csproj package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add src/CharityHealth.Infrastructure/CharityHealth.Infrastructure.csproj package BCrypt.Net-Next
dotnet add src/CharityHealth.Infrastructure/CharityHealth.Infrastructure.csproj package QRCoder

# ═══════════════════════════════════════════════════
# Default Admin Credentials (change immediately!)
# ═══════════════════════════════════════════════════
# Email:    admin@charityhealth.org
# Password: Admin@123456!
# ═══════════════════════════════════════════════════
# Beneficiary
# ═══════════════════════════════════════════════════
# Email:    mm0115810@gmail.com             
# Password: Admin@123456!


## UI Migration Patch
تم إضافة واجهة Blazor RTL حديثة وربطها بخدمات قراءة/تحديث بيانات النظام داخل CharityHealth.Web. راجع UI_MIGRATION_NOTES.md.
