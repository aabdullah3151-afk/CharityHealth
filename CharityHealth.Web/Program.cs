using CharityHealth.Web.Services;
using CharityHealth.Application;
using CharityHealth.Infrastructure;
using CharityHealth.Web.Data;
using CharityHealth.Web.Hubs;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
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


// ── Uploads ───────────────────────────────────────────
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
    var antiforgery =
        context.RequestServices.GetRequiredService<IAntiforgery>();

    antiforgery.GetAndStoreTokens(context);

    await next();
});


app.UseAuthentication();
app.UseAuthorization();


// Controllers + Razor Pages
app.MapControllers();

app.MapRazorPages();


app.UseAntiforgery();


// ── Blazor & SignalR ──────────────────────────────────
app.MapBlazorHub();

app.MapHub<NotificationHub>("/hubs/notifications");


app.MapFallbackToPage("/_Host");


// ── Auto Create Database on Render ─────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<CharityHealth.Infrastructure.Persistence.AppDbContext>();

    db.Database.EnsureCreated();
}


app.Run();