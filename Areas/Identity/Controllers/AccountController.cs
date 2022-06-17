using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using App.Areas.Identity.Models.AccountViewModels;
using App.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace App.Areas.Identity.Controllers
{
    [Authorize]
    [Area("Identity")]
    [Route("Account/[action]")]
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly IEmailSender _emailSender;
        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ILogger<AccountController> logger, IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (ModelState.IsValid)
            {
                var user = new AppUser { UserName = model.UserName, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // _logger.LogInformation("Đã tạo user mới.");

                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.ActionLink(
                        action: "ConfirmEmail",
                        controller: "account",
                        values: new { area = "Identity", userId = user.Id, code = code },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(model.Email, "Xác nhận địa chỉ email",
                        $"Bạn đã đăng ký tài khoản trên AppMvc hãy <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>bấm vào đây</a> để kích hoạt tài khoản.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return View("RegisterConfirmation");
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return View();
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {

            if (userId == null || code == null)
            {
                return View("ErrorConfirmEmail");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, false);
                return LocalRedirect("~/");
            }
            else
            {
                return Content("Lỗi xác thực email.");
            }

        }

        public IActionResult RegisterConfirmation(string returnUrl = null)
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> Login(string returnUrl = null)
        {
            //returnUrl ??= Url.Content("~/");
            ViewData["ReturnUrl"] = returnUrl;
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            return View();
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ExternalLogin()
        {
            return View("Login");
        }
        [HttpPost]
        [AllowAnonymous]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.

            var redirectUrl = Url.ActionLink(
                        action: "ExternalLoginCallback",
                        controller: "account",
                        values: new { ReturnUrl = returnUrl },
                        protocol: Request.Scheme);
            returnUrl ??= Url.Content("~/");
            // var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi sử dụng dịch vụ ngoài: {remoteError}");
                return View("Login");
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ModelState.AddModelError(string.Empty, $"Không lấy được thông tin từ dịch vụ ngoài.: {remoteError}");
                return View("Login");
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity.Name, info.LoginProvider);
                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToAction("Lockout");
            }
            else
            {

                // If the user does not have an account, then ask the user to create an account.
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["ProviderDisplayName"] = info.ProviderDisplayName;
                var email = new ExternalLoginViewModel();
                if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
                {
                    email.Email = info.Principal.FindFirstValue(ClaimTypes.Email);
                }
                return View("ExternalLogin", email);
            }
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginViewModel model, string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            // Get the information about the user from the external login provider
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ModelState.AddModelError(string.Empty, "Lỗi lấy thông tin từ dịch vụ ngoài.");
                return RedirectToAction("Login", new { ReturnUrl = returnUrl });
            }

            if (ModelState.IsValid)
            {
                var registeredUser = await _userManager.FindByEmailAsync(model.Email);
                string externalEmail = null;
                AppUser externalEmailUser = null;
                if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
                {
                    externalEmail = info.Principal.FindFirstValue(ClaimTypes.Email);
                }

                if (externalEmail != null)
                {
                    externalEmailUser = await _userManager.FindByEmailAsync(externalEmail);
                }

                if ((registeredUser != null) && (externalEmailUser != null))
                {
                    if (registeredUser.Id == externalEmailUser.Id)
                    {
                        var resultLink = await _userManager.AddLoginAsync(registeredUser, info);
                        if (resultLink.Succeeded)
                        {
                            await _signInManager.SignInAsync(registeredUser, isPersistent: false);
                            return LocalRedirect(returnUrl);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Không liên kết được tài khoản, hãy sử dụng email khác");
                        return View("ExternalLogin");
                    }
                }
                if ((externalEmailUser != null) && (registeredUser == null))
                {
                    ModelState.AddModelError(string.Empty, "Không hỗ trợ tạo tài khoản mới - có email khác từ dịch vụ ngoài");
                    return View("ExternalLogin");
                }
                if ((externalEmailUser == null) && (externalEmail == model.Email))
                {
                    var newUser = new AppUser()
                    {
                        UserName = externalEmail,
                        Email = externalEmail
                    };
                    var resultNewuser = await _userManager.CreateAsync(newUser);
                    if (resultNewuser.Succeeded)
                    {
                        await _userManager.AddLoginAsync(newUser, info);
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);
                        await _userManager.ConfirmEmailAsync(newUser, code);
                        await _signInManager.SignInAsync(newUser, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Không tạo được tài khoản mới");
                    return View("ExternalLogin");
                }

                var user = new AppUser { UserName = model.Email, Email = model.Email };

                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    // Thực hiện liên kết tài khoản ngoài
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

                        var userId = await _userManager.GetUserIdAsync(user);
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        var callbackUrl = Url.ActionLink(
                           action: "ConfirmEmail",
                           controller: "account",
                           values: new { area = "Identity", userId = user.Id, code = code },
                           protocol: Request.Scheme);

                        await _emailSender.SendEmailAsync(model.Email, "Confirm your email",
                            $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                        // If account confirmation is required, we need to show the link if we don't have a real email sender
                        if (_userManager.Options.SignIn.RequireConfirmedAccount)
                        {
                            return View("RegisterConfirmation");
                        }

                        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);

                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewData["ProviderDisplayName"] = info.ProviderDisplayName;
            ViewData["ReturnUrl"] = returnUrl;
            return View("ExternalLogin");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Logout()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Logout(string returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                returnUrl = Url.Content("~/");
                return LocalRedirect(returnUrl);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(model.UserNameOrEmail, model.Password, model.RememberMe, lockoutOnFailure: true);

                if (!result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.UserNameOrEmail);
                    if (user != null)
                    {
                        result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);
                    }
                }
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToAction("LoginWith2faSendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Tài khoản bị khóa.");
                    return View("Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Thất bại, tài khoản không tồn tại hoặc sai Tên đăng nhập, mật khẩu");
                    return View();
                }
            }

            // If we got this far, something failed, redisplay form
            return View();
        }

        public async Task<IActionResult> ConfirmEmailChane(string userId, string email, string code)
        {
            if (userId == null || email == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var oldEmail = user.Email;
            var result = await _userManager.ChangeEmailAsync(user, email, code);
            if (!result.Succeeded)
            {
                return View();
            }

            // In our UI email and user name are one and the same, so when we update the email
            // we need to update the user name.
            if (user.UserName == oldEmail)
            {
                var setUserNameResult = await _userManager.SetUserNameAsync(user, email);
                if (!setUserNameResult.Succeeded)
                {
                    return View();
                }
            }


            await _signInManager.RefreshSignInAsync(user);
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation", "Không tìm thấy user trên");
                }

                // For more information on how to enable account confirmation and password reset please 
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);

                // var decodedTokenString = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.ActionLink(
                    action: "ResetPassword",
                    controller: "account",
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(
                    model.Email,
                    "Reset Password",
                    $"Hãy bấm  <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>vào đây</a> để đặt lại mật khẩu.");
                return View("ForgotPasswordConfirmation");
            }
            return View();
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            if (code == null)
            {
                return BadRequest("Mã token không chính xác");
            }
            else
            {
                return View();
            }
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPassworViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                ModelState.AddModelError(string.Empty, "Không tìm thấy user trên");
                return View("ForgotPasswor");
            }
            var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));
            var result = await _userManager.ResetPasswordAsync(user, code, model.Password);
            if (result.Succeeded)
            {
                return View("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResendEmailConfirmation()
        {
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResendEmailConfirmation(ExternalLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Không tìm thấy địa chỉ email.");
                return View();
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.ActionLink(
                action: "ConfirmEmail",
                controller: "account",
                values: new { area = "Identity", userId = user.Id, code = code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(model.Email, "Xác nhận địa chỉ email",
                $"Bạn đã đăng ký tài khoản trên AppMvc hãy <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>bấm vào đây</a> để kích hoạt tài khoản.");

            ModelState.AddModelError(string.Empty, "Verification email sent. Please check your email.");
            return View();
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> LoginWith2faSendCode(string returnUrl = null, bool rememberMe = false)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            var userFactors = await _userManager.GetValidTwoFactorProvidersAsync(user);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new LoginWith2faSendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWith2faSendCode(LoginWith2faSendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            // Dùng mã Authenticator
            if (model.SelectedProvider == "Authenticator")
            {
                return RedirectToAction(nameof(VerifyAuthenticatorCode), new { ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
            }

            // Generate the token and send it
            var code = await _userManager.GenerateTwoFactorTokenAsync(user, model.SelectedProvider);
            if (string.IsNullOrWhiteSpace(code))
            {
                return View("Error");
            }

            var message = "Your security code is: " + code;
            if (model.SelectedProvider == "Email")
            {
                await _emailSender.SendEmailAsync(await _userManager.GetEmailAsync(user), "Security Code", message);
            }
            else if (model.SelectedProvider == "Phone")
            {
                await _emailSender.SendSmsAsync(await _userManager.GetPhoneNumberAsync(user), message);
            }

            return RedirectToAction(nameof(LoginWith2faVerifyCode), new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWith2faVerifyCode(string provider, bool rememberMe, string returnUrl = null)
        {
            // Require that the user has already logged in via username/password or external login
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            return View(new LoginWith2faVerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWith2faVerifyCode(LoginWith2faVerifyCodeViewModel model)
        {
            model.ReturnUrl ??= Url.Content("~/");
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes.
            // If a user enters incorrect codes for a specified amount of time then the user account
            // will be locked out for a specified amount of time.
            var result = await _signInManager.TwoFactorSignInAsync(model.Provider, model.Code, model.RememberMe, model.RememberBrowser);
            if (result.Succeeded)
            {
                return LocalRedirect(model.ReturnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning(7, "User account locked out.");
                return View("Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid code.");
                return View(model);
            }
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyAuthenticatorCode(bool rememberMe, string returnUrl = null)
        {
            // Require that the user has already logged in via username/password or external login
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            return View(new VerifyAuthenticatorCodeViewModel { ReturnUrl = returnUrl, RememberMe = rememberMe });
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyAuthenticatorCode(VerifyAuthenticatorCodeViewModel model)
        {
            model.ReturnUrl ??= Url.Content("~/");
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes.
            // If a user enters incorrect codes for a specified amount of time then the user account
            // will be locked out for a specified amount of time.
            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(model.Code, model.RememberMe, model.RememberBrowser);
            if (result.Succeeded)
            {
                return LocalRedirect(model.ReturnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning(7, "User account locked out.");
                return View("Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Mã sai.");
                return View(model);
            }
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> UseRecoveryCode(string returnUrl = null)
        {
            // Require that the user has already logged in via username/password or external login
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            return View(new UseRecoveryCodeViewModel { ReturnUrl = returnUrl });
        }

        //
        // POST: /Account/UseRecoveryCode
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> UseRecoveryCode(UseRecoveryCodeViewModel model)
        {
            model.ReturnUrl ??= Url.Content("~/");
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(model.Code);
            if (result.Succeeded)
            {
                return LocalRedirect(model.ReturnUrl);
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Sai mã phục hồi.");
                return View(model);
            }
        }
        [Route("/khongduoctruycap.html")]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}