using CharityHealth.Application.Interfaces.Services;
using CharityHealth.Domain.Entities;
using CharityHealth.Domain.Enums;
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

        // LOGIN_ERROR_MESSAGE_PATCH
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromForm] string credential,
            [FromForm] string password,
            [FromForm] string? returnUrl = null)
        {
            credential = credential?.Trim() ?? string.Empty;

            try
            {
                var user = await userManager.FindByEmailAsync(credential)
                    ?? await userManager.FindByNameAsync(credential)
                    ?? userManager.Users.FirstOrDefault(
                        u => u.PhoneNumber == credential);

                if (user is null || !user.IsActive)
                {
                    return RedirectToLogin(
                        "البريد الإلكتروني أو رقم الهاتف أو كلمة المرور غير صحيحة.",
                        returnUrl);
                }

                if (await userManager.IsLockedOutAsync(user))
                {
                    return RedirectToLogin(
                        "الحساب مقفل مؤقتًا. حاول مرة أخرى بعد 15 دقيقة.",
                        returnUrl);
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

                    return RedirectToLogin(
                        "البريد الإلكتروني أو رقم الهاتف أو كلمة المرور غير صحيحة.",
                        returnUrl);
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

                logger.LogInformation(
                    "User {UserId} logged in via password",
                    user.Id);

                return Redirect(
                    GetSafeReturnUrl(
                        returnUrl,
                        user.UserType,
                        roles));
            }
            catch (Exception ex)
            {
                try
                {
                    await signInManager.SignOutAsync();
                }
                catch
                {
                }

                // الخطأ الكامل سيظهر داخل Terminal
                logger.LogError(
                    ex,
                    "Technical login failure for credential {Credential}",
                    credential);

                var technicalMessage =
                    ex.GetBaseException().Message;

                var userMessage =
                    technicalMessage.Contains(
                        "127.0.0.1:5432",
                        System.StringComparison.OrdinalIgnoreCase)
                    || technicalMessage.Contains(
                        "Connection refused",
                        System.StringComparison.OrdinalIgnoreCase)
                    || technicalMessage.Contains(
                        "Failed to connect",
                        System.StringComparison.OrdinalIgnoreCase)

                    ? "تعذر تسجيل الدخول لأن قاعدة البيانات غير متاحة حاليًا. تأكد من تشغيل PostgreSQL ثم حاول مرة أخرى."

                    : "حدث خطأ تقني أثناء تسجيل الدخول. حاول مرة أخرى بعد قليل.";

                return RedirectToLogin(
                    userMessage,
                    returnUrl);
            }
        }

        private IActionResult RedirectToLogin(
            string message,
            string? returnUrl = null)
        {
            var target =
                "/login?error="
                + System.Uri.EscapeDataString(message);

            if (!string.IsNullOrWhiteSpace(returnUrl)
                && Url.IsLocalUrl(returnUrl))
            {
                target +=
                    "&returnUrl="
                    + System.Uri.EscapeDataString(returnUrl);
            }

            return Redirect(target);
        }


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

        private async Task SignInUserAsync(ApplicationUser user, IList<string> roles)
        {
            var resolvedUserType = ResolveUserType(user.UserType, roles);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? user.PhoneNumber ?? user.Id),

                new("FullNameAr", user.FullNameAr ?? string.Empty),
                new("FullNameEn", user.FullNameEn ?? string.Empty),
                new("UserType", resolvedUserType)
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

            await HttpContext.SignInAsync(
                IdentityConstants.ApplicationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    AllowRefresh = true
                });
        }

        private static string GetSafeReturnUrl(
            string? returnUrl,
            UserType userType,
            IList<string> roles)
        {
            var fallback = GetDefaultUrl(userType, roles);

            if (string.IsNullOrWhiteSpace(returnUrl)
                || !returnUrl.StartsWith('/')
                || returnUrl.StartsWith("//")
                || returnUrl.Equals("/", StringComparison.OrdinalIgnoreCase))
            {
                return fallback;
            }

            if (returnUrl.StartsWith("/admin", StringComparison.OrdinalIgnoreCase)
                && !HasRole(roles, "Administrator")
                && !HasRole(roles, "Staff"))
            {
                return fallback;
            }

            if (returnUrl.StartsWith("/staff", StringComparison.OrdinalIgnoreCase)
                && !HasRole(roles, "Staff"))
            {
                return fallback;
            }

            if (returnUrl.StartsWith("/doctor", StringComparison.OrdinalIgnoreCase)
                && !HasRole(roles, "Doctor"))
            {
                return fallback;
            }

            if (returnUrl.StartsWith("/portal", StringComparison.OrdinalIgnoreCase)
                && !HasRole(roles, "Beneficiary"))
            {
                return fallback;
            }

            if (returnUrl.StartsWith("/pharmacy", StringComparison.OrdinalIgnoreCase)
                && !HasRole(roles, "Pharmacist")
                && !HasRole(roles, "Pharmacy"))
            {
                return fallback;
            }

            if (returnUrl.StartsWith("/laboratory", StringComparison.OrdinalIgnoreCase)
                && !HasRole(roles, "Laboratory"))
            {
                return fallback;
            }

            if (returnUrl.StartsWith("/radiology", StringComparison.OrdinalIgnoreCase)
                && !HasRole(roles, "RadiologyCenter"))
            {
                return fallback;
            }

            return returnUrl;
        }

        private static string GetDefaultUrl(UserType userType, IList<string> roles)
        {
            if (HasRole(roles, "Laboratory"))
            {
                return "/laboratory/dashboard";
            }

            if (HasRole(roles, "RadiologyCenter"))
            {
                return "/radiology/dashboard";
            }

            if (HasRole(roles, "Pharmacy") || HasRole(roles, "Pharmacist"))
            {
                return "/pharmacy/dashboard";
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
                UserType.Laboratory => "/laboratory/dashboard",
                UserType.RadiologyCenter => "/radiology/dashboard",
                UserType.Pharmacy => "/pharmacy/dashboard",
                UserType.Pharmacist => "/pharmacy/dashboard",
                _ => "/"
            };
        }

        private static string ResolveUserType(UserType userType, IList<string> roles)
        {
            if (HasRole(roles, "Laboratory"))
            {
                return "Laboratory";
            }

            if (HasRole(roles, "RadiologyCenter"))
            {
                return "RadiologyCenter";
            }

            if (HasRole(roles, "Pharmacy"))
            {
                return "Pharmacy";
            }

            if (HasRole(roles, "Pharmacist"))
            {
                return "Pharmacist";
            }

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
