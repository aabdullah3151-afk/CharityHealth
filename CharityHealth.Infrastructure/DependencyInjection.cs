using CharityHealth.Application.Interfaces.Services;
using CharityHealth.Domain.Entities;
using CharityHealth.Domain.Interfaces.Repositories;
using CharityHealth.Infrastructure.Persistence;
using CharityHealth.Infrastructure.Repositories;
using CharityHealth.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CharityHealth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddIdentity<ApplicationUser, IdentityRole>(opts =>
        {
            opts.Password.RequiredLength         = 6;
 opts.Password.RequireUppercase       = false;
 opts.Password.RequireDigit           = false;
 opts.Password.RequireNonAlphanumeric = false;
 opts.Password.RequireLowercase       = false;
 opts.Lockout.MaxFailedAccessAttempts = 5;
 opts.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(15);
 opts.User.RequireUniqueEmail          = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ISmsSender, ConsoleSmsStub>();
        services.Configure<FileStorageSettings>(configuration.GetSection("FileStorage"));
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
       // services.AddScoped<INotificationSender, NotificationSender>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<IBeneficiaryRepository, BeneficiaryRepository>();

        return services;
    }
}
