using CharityHealth.Web.Services;
using CharityHealth.Application;
using CharityHealth.Infrastructure;
using CharityHealth.Web.Data;
using CharityHealth.Web.Hubs;
using CharityHealth.Domain.Entities;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Radzen;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddAuthorization();
builder.Services.AddControllers();


// ── Layers ────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);


// ── Radzen ────────────────────────────────────────────
builder.Services.AddRadzenComponents();
builder.Services.AddScoped<Radzen.DialogService>();
builder.Services.AddScoped<Radzen.NotificationService>();
builder.Services.AddScoped<Radzen.TooltipService>();


// ── Auth ──────────────────────────────────────────────
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<
    AuthenticationStateProvider,
    Microsoft.AspNetCore.Components.Server.ServerAuthenticationStateProvider>();

builder.Services.AddScoped<
    CharityHealth.Application.Interfaces.Services.INotificationSender,
    CharityHealth.Web.Hubs.SignalRNotificationSender>();


// ── SignalR ───────────────────────────────────────────
builder.Services.AddSignalR();


// ── HttpContextAccessor ───────────────────────────────
builder.Services.AddHttpContextAccessor();


// ── UI Services ───────────────────────────────────────
builder.Services.AddScoped<UiThemeService>();
builder.Services.AddScoped<HealthcareUiService>();


// ── Localization ──────────────────────────────────────
builder.Services.AddLocalization(opts =>
{
    opts.ResourcesPath = "Resources";
});


// ── Cookie Auth ───────────────────────────────────────
builder.Services.ConfigureApplicationCookie(opts =>
{
    opts.LoginPath = "/login";
    opts.AccessDeniedPath = "/access-denied";

    opts.ExpireTimeSpan = TimeSpan.FromDays(30);
    opts.SlidingExpiration = true;

    opts.Cookie.HttpOnly = true;
    opts.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    opts.Cookie.SameSite = SameSiteMode.Lax;
});


// ── HSTS ──────────────────────────────────────────────
builder.Services.AddHsts(opts =>
{
    opts.Preload = true;
    opts.IncludeSubDomains = true;
    opts.MaxAge = TimeSpan.FromDays(365);
});


var app = builder.Build();


// ── Middleware Pipeline ───────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}


app.UseHttpsRedirection();

app.UseStaticFiles();


// ── Uploads ───────────────────────────────────────────
var uploadsPath = builder.Configuration["FileStorage:BasePath"];

if (string.IsNullOrWhiteSpace(uploadsPath))
{
    uploadsPath = Path.Combine(
        app.Environment.ContentRootPath,
        "uploads");
}
else if (!Path.IsPathRooted(uploadsPath))
{
    uploadsPath = Path.Combine(
        app.Environment.ContentRootPath,
        uploadsPath);
}

Directory.CreateDirectory(uploadsPath);


app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});


app.UseRouting();


app.Use(async (context, next) =>
{
    var antiforgery =
        context.RequestServices.GetRequiredService<IAntiforgery>();

    antiforgery.GetAndStoreTokens(context);

    await next();
});


app.UseAuthentication();


// ── Redirect authenticated users from home/login ─────
app.Use(async (context, next) =>
{
    if ((context.Request.Path == "/" ||
         context.Request.Path == "/login")
        && context.User.Identity?.IsAuthenticated == true)
    {
        var user = context.User;

        string? target = null;

        if (user.IsInRole("Laboratory"))
        {
            target = "/laboratory/dashboard";
        }
        else if (user.IsInRole("RadiologyCenter"))
        {
            target = "/radiology/dashboard";
        }
        else if (user.IsInRole("Pharmacy") ||
                 user.IsInRole("Pharmacist"))
        {
            target = "/pharmacy/dashboard";
        }
        else if (user.IsInRole("Administrator") ||
                 user.IsInRole("Staff"))
        {
            target = "/admin/dashboard";
        }
        else if (user.IsInRole("Doctor"))
        {
            target = "/doctor/dashboard";
        }
        else if (user.IsInRole("Beneficiary"))
        {
            target = "/portal/dashboard";
        }
        else
        {
            var userType =
                user.FindFirst("UserType")?.Value;

            target = userType switch
            {
                "Administrator" => "/admin/dashboard",
                "Staff" => "/admin/dashboard",
                "Doctor" => "/doctor/dashboard",
                "Beneficiary" => "/portal/dashboard",
                "Pharmacy" => "/pharmacy/dashboard",
                "Pharmacist" => "/pharmacy/dashboard",
                "Laboratory" => "/laboratory/dashboard",
                "RadiologyCenter" => "/radiology/dashboard",
                _ => null
            };
        }

        if (!string.IsNullOrWhiteSpace(target))
        {
            context.Response.Redirect(target);
            return;
        }
    }

    await next();
});


app.UseAuthorization();


// ── Controllers + Razor Pages ─────────────────────────
app.MapControllers();

app.MapRazorPages();

app.UseAntiforgery();


// ── Blazor & SignalR ──────────────────────────────────
app.MapBlazorHub();

app.MapHub<NotificationHub>("/hubs/notifications");

app.MapFallbackToPage("/_Host");


// ── Auto Apply EF Core Migrations ─────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<
            CharityHealth.Infrastructure.Persistence.AppDbContext>();

    db.Database.Migrate();

    // Ensure required Identity roles always exist
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    var requiredRoles = new[]
    {
        "Administrator",
        "Staff",
        "Doctor",
        "Pharmacy",
        "Pharmacist",
        "Laboratory",
        "RadiologyCenter"
    };

    foreach (var roleName in requiredRoles)
    {
        if (await roleManager.RoleExistsAsync(roleName))
            continue;

        var roleResult = await roleManager.CreateAsync(
            new IdentityRole(roleName));

        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create role {roleName}: " +
                string.Join(
                    " | ",
                    roleResult.Errors.Select(x => x.Description)));
        }
    }
}


// ── Temporary Admin Password Reset ────────────────────
var adminResetPassword =
    builder.Configuration["ADMIN_RESET_PASSWORD"];

if (!string.IsNullOrWhiteSpace(adminResetPassword))
{
    using var resetScope = app.Services.CreateScope();

    var userManager = resetScope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

    var admin = await userManager.FindByEmailAsync(
        "admin@charityhealth.org");

    if (admin is null)
    {
        throw new InvalidOperationException(
            "The administrator account was not found.");
    }

    admin.PasswordHash =
        userManager.PasswordHasher.HashPassword(
            admin,
            adminResetPassword);

    admin.SecurityStamp = Guid.NewGuid().ToString();

    var updateResult =
        await userManager.UpdateAsync(admin);

    if (!updateResult.Succeeded)
    {
        var errors = string.Join(
            " | ",
            updateResult.Errors.Select(
                error => $"{error.Code}: {error.Description}"));

        throw new InvalidOperationException(
            $"Administrator password reset failed: {errors}");
    }

    await userManager.ResetAccessFailedCountAsync(admin);

    await userManager.SetLockoutEndDateAsync(
        admin,
        null);

    app.Logger.LogWarning(
        "Administrator password reset completed successfully.");
}


app.Run();