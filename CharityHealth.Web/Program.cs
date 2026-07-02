using CharityHealth.Application;
using CharityHealth.Infrastructure;
using CharityHealth.Web.Data;
using CharityHealth.Web.Hubs;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Microsoft.Extensions.FileProviders;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddAuthorization();
builder.Services.AddControllers();
// ── Layers ────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── Blazor Server ─────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── Radzen ────────────────────────────────────────────
builder.Services.AddRadzenComponents();
builder.Services.AddScoped<Radzen.DialogService>();
builder.Services.AddScoped<Radzen.NotificationService>();
builder.Services.AddScoped<Radzen.TooltipService>();

// ── Auth ──────────────────────────────────────────────
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider,
    Microsoft.AspNetCore.Components.Server.ServerAuthenticationStateProvider>();

builder.Services.AddScoped<
    CharityHealth.Application.Interfaces.Services.INotificationSender,
    CharityHealth.Web.Hubs.SignalRNotificationSender>();

// ── SignalR ───────────────────────────────────────────
builder.Services.AddSignalR();

// ── HttpContextAccessor (needed for CurrentUserService) ──
builder.Services.AddHttpContextAccessor();

// ── Localization ──────────────────────────────────────
builder.Services.AddLocalization(opts => opts.ResourcesPath = "Resources");

// ── Cookie Auth ───────────────────────────────────────
builder.Services.ConfigureApplicationCookie(opts =>
{
    opts.LoginPath = "/login";
    opts.AccessDeniedPath = "/access-denied";
    opts.ExpireTimeSpan = TimeSpan.FromHours(8);
    opts.SlidingExpiration = true;
    opts.Cookie.HttpOnly = true;
    opts.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    opts.Cookie.SameSite = SameSiteMode.Strict;
});

// ── HSTS ─────────────────────────────────────────────
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
// ── Serve uploaded files ───────────────────────────────
// ✅ Auto-create the uploads directory so PhysicalFileProvider never throws
var uploadsPath = builder.Configuration["FileStorage:BasePath"];

if (string.IsNullOrWhiteSpace(uploadsPath))
{
    uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
}
else if (!Path.IsPathRooted(uploadsPath))
{
    uploadsPath = Path.Combine(app.Environment.ContentRootPath, uploadsPath);
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
    var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();

    antiforgery.GetAndStoreTokens(context);

    await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.UseAntiforgery();

// ── Blazor & SignalR ──────────────────────────────────
//app.MapRazorComponents<CharityHealth.Web.App>()
//    .AddInteractiveServerRenderMode();
app.MapBlazorHub();
//app.MapHub<NotificationHub>("/hubs/notifications");

app.MapFallbackToPage("/_Host");

app.Run();

// ─────────────────────────────────────────────────────





