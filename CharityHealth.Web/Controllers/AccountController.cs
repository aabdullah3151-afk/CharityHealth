using CharityHealth.Application.Interfaces.Services;
using CharityHealth.Domain.Entities;
using CharityHealth.Domain.Enums;
using CharityHealth.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CharityHealth.Web.Controllers
{
    [Route("account")]
    public class AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOtpService otpService,
        IAuditService audit,
        ILogger<AccountController> logger) : Controller
    {
        // ──────────────────────────────────────────────────────
        // POST /account/login  (Username/Email/Phone + Password)
        // ──────────────────────────────────────────────────────
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromForm] string credential,
            [FromForm] string password,
            [FromForm] string? returnUrl = null)
        {
            credential = credential?.Trim() ?? string.Empty;

            var user = await userManager.FindByEmailAsync(credential)
                ?? await userManager.FindByNameAsync(credential)
                ?? userManager.Users.FirstOrDefault(u => u.PhoneNumber == credential);

            if (user is null || !user.IsActive)
            {
                TempData["LoginError"] = "اسم المستخدم أو كلمة المرور غير صحيحة";
                return Redirect("/login");
            }

            if (await userManager.IsLockedOutAsync(user))
            {
                TempData["LoginError"] = "الحساب مقفل مؤقتاً. حاول بعد 15 دقيقة.";
                return Redirect("/login");
            }

            if (!await userManager.CheckPasswordAsync(user, password))
            {
                await userManager.AccessFailedAsync(user);

                await audit.LogAsync(
                    "Auth.Login.Failed",
                    "ApplicationUser",
                    user.Id,
                    newValues: "{\"reason\":\"wrong_password\"}"
                );

                TempData["LoginError"] = "اسم المستخدم أو كلمة المرور غير صحيحة";
                return Redirect("/login");
            }

            await userManager.ResetAccessFailedCountAsync(user);

            var roles = await userManager.GetRolesAsync(user);

            await SignInUserAsync(user, roles);

            await audit.LogAsync(
                "Auth.Login.Success",
                "ApplicationUser",
                user.Id,
                newValues: "{\"method\":\"password\"}"
            );

            logger.LogInformation("User {UserId} logged in via password", user.Id);

            return Redirect(GetSafeReturnUrl(returnUrl, user.UserType, roles));
        }

        // ──────────────────────────────────────────────────────
        // POST /account/send-otp
        // ──────────────────────────────────────────────────────
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromForm] string phone)
        {
            phone = phone?.Trim() ?? string.Empty;

            var user = userManager.Users.FirstOrDefault(u => u.PhoneNumber == phone);

            if (user is not null && user.IsActive)
            {
                await otpService.SendOtpAsync(user.Id, phone);
            }

            TempData["OtpPhone"] = phone;
            TempData["OtpSent"] = "true";

            return Redirect("/login?tab=otp");
        }

        // ──────────────────────────────────────────────────────
        // POST /account/verify-otp
        // ──────────────────────────────────────────────────────
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(
            [FromForm] string phone,
            [FromForm] string otpCode,
            [FromForm] string? returnUrl = null)
        {
            phone = phone?.Trim() ?? string.Empty;
            otpCode = otpCode?.Trim() ?? string.Empty;

            var verifyResult = await otpService.VerifyOtpAsync(phone, otpCode);

            if (!verifyResult.Success)
            {
                TempData["OtpPhone"] = phone;
                TempData["OtpSent"] = "true";
                TempData["OtpError"] = verifyResult.IsLocked
                    ? "تم تجاوز المحاولات المسموحة. حاول بعد 15 دقيقة."
                    : verifyResult.ErrorMessage;

                return Redirect("/login?tab=otp");
            }

            var user = userManager.Users.FirstOrDefault(u => u.PhoneNumber == phone);

            if (user is null || !user.IsActive)
            {
                TempData["OtpError"] = "المستخدم غير موجود";
                return Redirect("/login?tab=otp");
            }

            var roles = await userManager.GetRolesAsync(user);

            await SignInUserAsync(user, roles);

            await audit.LogAsync(
                "Auth.Login.Success",
                "ApplicationUser",
                user.Id,
                newValues: "{\"method\":\"otp\"}"
            );

            logger.LogInformation("User {UserId} logged in via OTP", user.Id);

            return Redirect(GetSafeReturnUrl(returnUrl, user.UserType, roles));
        }

        // ──────────────────────────────────────────────────────
        // POST /account/logout
        // ──────────────────────────────────────────────────────
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await signInManager.SignOutAsync();

            if (userId is not null)
            {
                await audit.LogAsync("Auth.Logout", "ApplicationUser", userId);
            }

            logger.LogInformation("User {UserId} logged out", userId);

            return Redirect("/login");
        }

        // ──────────────────────────────────────────────────────
        // GET /account/logout
        // ──────────────────────────────────────────────────────
        [HttpGet("logout")]
        public async Task<IActionResult> LogoutGet()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await signInManager.SignOutAsync();

            if (userId is not null)
            {
                await audit.LogAsync("Auth.Logout", "ApplicationUser", userId);
            }

            return Redirect("/login");
        }

        // ══════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ══════════════════════════════════════════════════════

        private async Task SignInUserAsync(ApplicationUser user, IList<string> roles)
        {
            var resolvedUserType = ResolveUserType(user.UserType, roles);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? user.PhoneNumber ?? user.Id),

                new("FullNameAr", user.FullNameAr ?? string.Empty),
                new("FullNameEn", user.FullNameEn ?? string.Empty),
                new("UserType", resolvedUserType),
            };

            var userClaims = await userManager.GetClaimsAsync(user);
            claims.AddRange(userClaims);

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));

                var roleEntity = await roleManager.FindByNameAsync(role);

                if (roleEntity is not null)
                {
                    var roleClaims = await roleManager.GetClaimsAsync(roleEntity);
                    claims.AddRange(roleClaims);
                }
            }

            var identity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal);
        }

        private static string GetSafeReturnUrl(
            string? returnUrl,
            UserType userType,
            IList<string> roles)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl)
                && returnUrl.StartsWith('/')
                && !returnUrl.StartsWith("//")
                && !returnUrl.Equals("/", StringComparison.OrdinalIgnoreCase))
            {
                return returnUrl;
            }

            if (HasRole(roles, "Administrator"))
            {
                return "/admin/dashboard";
            }

            if (HasRole(roles, "Staff"))
            {
                return "/admin/dashboard";
            }

            if (HasRole(roles, "Doctor"))
            {
                return "/doctor/dashboard";
            }

            if (HasRole(roles, "Beneficiary"))
            {
                return "/portal/dashboard";
            }

            return userType switch
            {
                UserType.Administrator => "/admin/dashboard",
                UserType.Staff => "/admin/dashboard",
                UserType.Doctor => "/doctor/dashboard",
                UserType.Beneficiary => "/portal/dashboard",
                _ => "/"
            };
        }

        private static string ResolveUserType(UserType userType, IList<string> roles)
        {
            if (HasRole(roles, "Administrator"))
            {
                return "Administrator";
            }

            if (HasRole(roles, "Staff"))
            {
                return "Staff";
            }

            if (HasRole(roles, "Doctor"))
            {
                return "Doctor";
            }

            if (HasRole(roles, "Beneficiary"))
            {
                return "Beneficiary";
            }

            return userType.ToString();
        }

        private static bool HasRole(IList<string> roles, string role)
        {
            return roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
        }
    }
}