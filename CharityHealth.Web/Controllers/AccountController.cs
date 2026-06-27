using CharityHealth.Application.Interfaces.Services;
using CharityHealth.Domain.DTO;
using CharityHealth.Domain.Entities;
using CharityHealth.Domain.Enums;
using CharityHealth.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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
            // ✅ Support email OR username OR phone number
            var user = await userManager.FindByEmailAsync(credential)
                    ?? await userManager.FindByNameAsync(credential)
                    ?? userManager.Users.FirstOrDefault(u => u.PhoneNumber == credential);

            if (user is null || !user.IsActive)
            {
                TempData["LoginError"] = "اسم المستخدم أو كلمة المرور غير صحيحة";
                return Redirect("/login");
            }

            // Check lockout
            if (await userManager.IsLockedOutAsync(user))
            {
                TempData["LoginError"] = "الحساب مقفل مؤقتاً. حاول بعد 15 دقيقة.";
                return Redirect("/login");
            }

            if (!await userManager.CheckPasswordAsync(user, password))
            {
                await userManager.AccessFailedAsync(user);
                await audit.LogAsync("Auth.Login.Failed", "ApplicationUser", user.Id,
                    newValues: "{\"reason\":\"wrong_password\"}");

                TempData["LoginError"] = "اسم المستخدم أو كلمة المرور غير صحيحة";
                return Redirect("/login");
            }

            // Reset failed attempts on success
            await userManager.ResetAccessFailedCountAsync(user);

            await SignInUserAsync(user);

            await audit.LogAsync("Auth.Login.Success", "ApplicationUser", user.Id,
                newValues: "{\"method\":\"password\"}");

            logger.LogInformation("User {UserId} logged in via password", user.Id);

            return Redirect(GetSafeReturnUrl(returnUrl, user.UserType));
        }

        // ──────────────────────────────────────────────────────
        // POST /account/send-otp
        // ──────────────────────────────────────────────────────
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromForm] string phone)
        {
            var user = userManager.Users.FirstOrDefault(u => u.PhoneNumber == phone);

            if (user is not null && user.IsActive)
                await otpService.SendOtpAsync(user.Id, phone);

            // Always return success (don't reveal if phone exists)
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

            await SignInUserAsync(user);

            await audit.LogAsync("Auth.Login.Success", "ApplicationUser", user.Id,
                newValues: "{\"method\":\"otp\"}");

            logger.LogInformation("User {UserId} logged in via OTP", user.Id);

            return Redirect(GetSafeReturnUrl(returnUrl, user.UserType));
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
                await audit.LogAsync("Auth.Logout", "ApplicationUser", userId);

            logger.LogInformation("User {UserId} logged out", userId);
            return Redirect("/login");
        }

        // ──────────────────────────────────────────────────────
        // GET /account/logout  (for direct navigation e.g. from NavMenu link)
        // ──────────────────────────────────────────────────────
        [HttpGet("logout")]
        public async Task<IActionResult> LogoutGet()
        {
            await signInManager.SignOutAsync();
            return Redirect("/login");
        }

        // ══════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Builds full Claims list and signs in via Identity cookie.
        /// ✅ Adds NameIdentifier + Name + FullNameAr + all role claims.
        /// </summary>
        private async Task SignInUserAsync(ApplicationUser user)
        {
            var claims = new List<Claim>
        {
            // ✅ These two are REQUIRED — Blazor AuthState reads them
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name,           user.UserName ?? user.PhoneNumber ?? user.Id),
 
            // Extra claims used in UI
            new("FullNameAr", user.FullNameAr),
            new("FullNameEn", user.FullNameEn),
            new("UserType",   user.UserType.ToString()),
        };

            // Add user-level claims from Identity
            var userClaims = await userManager.GetClaimsAsync(user);
            claims.AddRange(userClaims);

            // Add role claims
            var roles = await userManager.GetRolesAsync(user);
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

        /// <summary>
        /// Only allow relative returnUrls to prevent open redirect attacks.
        /// </summary>
        private static string GetSafeReturnUrl(string? returnUrl, UserType userType)
        {
            if (!string.IsNullOrEmpty(returnUrl)
                && returnUrl.StartsWith('/')
                && !returnUrl.StartsWith("//"))
                return returnUrl;

            return userType switch
            {
                UserType.Beneficiary => "/portal/dashboard",
                UserType.Doctor => "/doctor/dashboard",
                UserType.Staff => "/staff/dashboard",
                UserType.Administrator => "/admin/dashboard",
                _ => "/"
            };
        }
    }


}
