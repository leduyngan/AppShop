using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using App.Areas.Identity.Models.ManageViewNodels;
using App.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace App.Areas.Identity.Controllers
{
    [Authorize]
    [Area("Identity")]
    [Route("Account/[action]")]
    public class ManageController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<ManageController> _logger;
        private readonly IEmailSender _emailSender;
        private readonly UrlEncoder _urlEncoder;
        public ManageController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ILogger<ManageController> logger, IEmailSender emailSender, UrlEncoder urlEncoder)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _urlEncoder = urlEncoder;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }
            var userInfo = new IndexViewModel();
            userInfo = await IndexLoadAsync(user);
            return View(userInfo);
        }
        [HttpPost]
        public async Task<IActionResult> Index(IndexViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                var UserInfo = new IndexViewModel();
                UserInfo = await IndexLoadAsync(user);
                return View(UserInfo);
            }

            user.HomeAddress = model.profile.HomeAddress;
            user.PhoneNumber = model.PhoneNumber;
            user.BirthDate = model.profile.BirthDate;

            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);

            return View();

            // var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            // if (Input.PhoneNumber != phoneNumber)
            // {
            //     var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
            //     if (!setPhoneResult.Succeeded)
            //     {
            //         StatusMessage = "Unexpected error when trying to set phone number.";
            //         return RedirectToPage();
            //     }
            // }

            // await _signInManager.RefreshSignInAsync(user);
            // StatusMessage = "Hồ sơ của bạn đã được cập nhật";
            // return RedirectToPage();
        }

        private async Task<IndexViewModel> IndexLoadAsync(AppUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            var User = new IndexViewModel
            {
                PhoneNumber = phoneNumber,

                profile = new EditExtraProfileModel()
                {
                    HomeAddress = user.HomeAddress,
                    BirthDate = user.BirthDate,
                    UserName = userName
                }
            };
            return User;
        }
        [HttpGet]
        public async Task<IActionResult> Email()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var EmailInfo = await EmailLoadAsync(user);
            return View(EmailInfo);
        }
        [HttpPost]
        public async Task<IActionResult> EmailChange(EmailChangeViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                var EmailInfo = await EmailLoadAsync(user);
                return View("Email", EmailInfo);
            }

            var email = await _userManager.GetEmailAsync(user);
            if (model.NewEmail != email)
            {
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateChangeEmailTokenAsync(user, model.NewEmail);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.ActionLink(
                    action: "EmailConfirmChange",
                    controller: "Manage",
                    values: new { area = "Identity", userId = user.Id, email = model.NewEmail, code = code },
                     protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(model.NewEmail,
                    "Xác nhận địa chỉ email",
                    $"<a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>bấm vào đây</a> để đổi email.");


                StatusMessage = "Hãy mở hòm thư email mới để xác nhận.";
                return RedirectToAction("Index");
            }

            StatusMessage = "Thông báo Your email is unchanged.";
            return RedirectToAction("Index");
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> EmailConfirmChange(string userId, string email, string code)
        {
            if (userId == null || email == null || code == null)
            {
                StatusMessage = "Lỗi không tìm thấy email.";
                return RedirectToAction("Index", "Home");

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
                StatusMessage = "Error changing email.";
                return RedirectToAction("Index", "Home");
            }

            // In our UI email and user name are one and the same, so when we update the email
            // we need to update the user name.
            if (user.UserName == oldEmail)
            {
                var setUserNameResult = await _userManager.SetUserNameAsync(user, email);
                if (!setUserNameResult.Succeeded)
                {
                    StatusMessage = "Lỗi đổi địa chỉ email.";
                    return RedirectToAction("Index", "Home");
                }
            }


            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Thank you for confirming your email change.";
            return RedirectToAction("Index", "Home");
        }

        private async Task<EmailChangeViewModel> EmailLoadAsync(AppUser user)
        {
            var email = await _userManager.GetEmailAsync(user);
            var EmailInfo = new EmailChangeViewModel()
            {
                Email = email,
                EmailIsConfirmed = await _userManager.IsEmailConfirmedAsync(user)
            };
            return EmailInfo;


        }
        [HttpGet]
        public async Task<IActionResult> PassWordSet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var hasPassword = await _userManager.HasPasswordAsync(user);

            if (hasPassword)
            {
                return RedirectToAction("ChangePassword");
            }
            return View("PassWordSet");
        }
        [HttpPost]
        public async Task<IActionResult> PassWordSet(PassWordSetViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("PassWordSet");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var addPasswordResult = await _userManager.AddPasswordAsync(user, model.NewPassword);
            if (!addPasswordResult.Succeeded)
            {
                foreach (var error in addPasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your password has been set.";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var hasPassword = await _userManager.HasPasswordAsync(user);
            // if (!hasPassword)
            // {
            //     return RedirectToPage("./SetPassword");
            // }

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePassWordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View();
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("User changed their password successfully.");

            StatusMessage = "Đổi mật khẩu thành công";
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> ExternalLogins()
        {
            var exLogin = new ExternalLoginsViewModel();
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID 'user.Id'.");
            }

            exLogin.CurrentLogins = await _userManager.GetLoginsAsync(user);
            exLogin.OtherLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync())
                .Where(auth => exLogin.CurrentLogins.All(ul => auth.Name != ul.LoginProvider))
                .ToList();
            exLogin.ShowRemoveButton = user.PasswordHash != null || exLogin.CurrentLogins.Count > 1;
            return View(exLogin);
        }

        [HttpPost]
        public async Task<IActionResult> ExternalLoginsRemove(string loginProvider, string providerKey)
        {

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID 'user.Id'.");
            }

            var result = await _userManager.RemoveLoginAsync(user, loginProvider, providerKey);
            var exLogin = new ExternalLoginsViewModel();
            exLogin.CurrentLogins = await _userManager.GetLoginsAsync(user);
            exLogin.OtherLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync())
                .Where(auth => exLogin.CurrentLogins.All(ul => auth.Name != ul.LoginProvider))
                .ToList();
            exLogin.ShowRemoveButton = user.PasswordHash != null || exLogin.CurrentLogins.Count > 1;
            if (!result.Succeeded)
            {
                StatusMessage = "The external login was not removed.";
                return View("ExternalLogins", exLogin);
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "The external login was removed.";
            return View("ExternalLogins", exLogin);
        }
        [HttpPost]
        public async Task<IActionResult> ExternalLoginsLink(string provider)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // Request a redirect to the external login provider to link a login for the current user
            var redirectUrl = Url.Action(
                action: "ExternalLoginsLinkCallback",
                controller: "Manage");

            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, _userManager.GetUserId(User));
            return new ChallengeResult(provider, properties);
        }
        [HttpGet]
        public async Task<IActionResult> ExternalLoginsLinkCallback()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID 'user.Id'.");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync(user.Id);
            if (info == null)
            {
                throw new InvalidOperationException($"Unexpected error occurred loading external login info for user with ID '{user.Id}'.");
            }

            var result = await _userManager.AddLoginAsync(user, info);
            var exLogin = new ExternalLoginsViewModel();
            exLogin.CurrentLogins = await _userManager.GetLoginsAsync(user);
            exLogin.OtherLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync())
                .Where(auth => exLogin.CurrentLogins.All(ul => auth.Name != ul.LoginProvider))
                .ToList();
            exLogin.ShowRemoveButton = user.PasswordHash != null || exLogin.CurrentLogins.Count > 1;
            if (!result.Succeeded)
            {
                StatusMessage = "The external login was not added. External logins can only be associated with one account.";
                return View("ExternalLogins", exLogin);
            }

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            StatusMessage = "The external login was added.";
            return View("ExternalLogins", exLogin);
        }
        [HttpGet]
        public async Task<IActionResult> TwoFactorAuthentication()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }
            var twoFactor = new TwoFactorAuthenticationViewModel();
            twoFactor.HasAuthenticator = await _userManager.GetAuthenticatorKeyAsync(user) != null;
            twoFactor.Is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            twoFactor.IsMachineRemembered = await _signInManager.IsTwoFactorClientRememberedAsync(user);
            twoFactor.RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);

            return View("TwoFactorAuthentication", twoFactor);
        }
        [HttpGet]
        public async Task<IActionResult> EnableTwoFactorAuthenticator()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }
            var enTwoFactor = new EnableTwoFactorAuthenticatorViewModel();
            enTwoFactor = await LoadSharedKeyAndQrCodeUriAsync(user);


            return View(enTwoFactor);
        }
        [HttpPost]
        public async Task<IActionResult> EnableTwoFactorAuthenticator(EnableTwoFactorAuthenticatorViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var enTwoFactor = new EnableTwoFactorAuthenticatorViewModel();
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                enTwoFactor = await LoadSharedKeyAndQrCodeUriAsync(user);
                return View(enTwoFactor);
            }

            // Strip spaces and hypens
            var verificationCode = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

            var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!is2faTokenValid)
            {
                ModelState.AddModelError("model.Code", "Verification code is invalid.");
                enTwoFactor = await LoadSharedKeyAndQrCodeUriAsync(user);
                return View(enTwoFactor);
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            var userId = await _userManager.GetUserIdAsync(user);
            _logger.LogInformation("User with ID '{UserId}' has enabled 2FA with an authenticator app.", userId);

            StatusMessage = "Your authenticator app has been verified.";

            if (await _userManager.CountRecoveryCodesAsync(user) == 0)
            {
                var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
                enTwoFactor.RecoveryCodes = recoveryCodes.ToArray();
                var code = new ShowRecoveryCodesViewModel();
                code.RecoveryCodes = recoveryCodes.ToArray();
                return RedirectToAction("ShowRecoveryCodes", code);
            }
            else
            {
                return RedirectToAction("TwoFactorAuthentication");
            }
        }
        private async Task<EnableTwoFactorAuthenticatorViewModel> LoadSharedKeyAndQrCodeUriAsync(AppUser user)
        {
            // Load the authenticator key & QR code URI to display on the form
            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }
            var enTwoFactor = new EnableTwoFactorAuthenticatorViewModel();
            enTwoFactor.SharedKey = FormatKey(unformattedKey);

            var email = await _userManager.GetEmailAsync(user);
            enTwoFactor.AuthenticatorUri = GenerateQrCodeUri(email, unformattedKey);
            return enTwoFactor;
        }
        private string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            int currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
                currentPosition += 4;
            }
            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition));
            }

            return result.ToString().ToLowerInvariant();
        }
        private string GenerateQrCodeUri(string email, string unformattedKey)
        {
            var enTwoFactor = new EnableTwoFactorAuthenticatorViewModel();
            return string.Format(
                enTwoFactor.AuthenticatorUriFormat,
                _urlEncoder.Encode("AppMvc.Net"),
                _urlEncoder.Encode(email),
                unformattedKey);
        }

        [HttpGet]
        public async Task<IActionResult> GenerateRecoveryCodes()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            if (!isTwoFactorEnabled)
            {
                var userId = await _userManager.GetUserIdAsync(user);
                throw new InvalidOperationException($"Cannot generate recovery codes for user with ID '{userId}' because they do not have 2FA enabled.");
            }

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> GenerateRecoveryCodesOnPost()
        {
            var user = await _userManager.GetUserAsync(User);
            var code = new GenerateRecoveryCodesViewModel();
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            var userId = await _userManager.GetUserIdAsync(user);
            if (!isTwoFactorEnabled)
            {
                throw new InvalidOperationException($"Cannot generate recovery codes for user with ID '{userId}' as they do not have 2FA enabled.");
            }

            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            code.RecoveryCodes = recoveryCodes.ToArray();
            var codeRecovery = new ShowRecoveryCodesViewModel();
            codeRecovery.RecoveryCodes = recoveryCodes.ToArray();
            _logger.LogInformation("User with ID '{UserId}' has generated new 2FA recovery codes.", userId);
            StatusMessage = "You have generated new recovery codes.";

            return RedirectToAction("ShowRecoveryCodes", codeRecovery);
        }
        [HttpGet]
        public IActionResult ShowRecoveryCodes(ShowRecoveryCodesViewModel model)

        {
            if (model.RecoveryCodes == null || model.RecoveryCodes.Length == 0)
            {
                return RedirectToAction("TwoFactorAuthentication");
            }

            return View(model);
        }
        public async Task<IActionResult> Disable2fa()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!await _userManager.GetTwoFactorEnabledAsync(user))
            {
                throw new InvalidOperationException($"Cannot disable 2FA for user with ID '{_userManager.GetUserId(User)}' as it's not currently enabled.");
            }

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Disable2faOnPost()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var disable2faResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
            if (!disable2faResult.Succeeded)
            {
                throw new InvalidOperationException($"Unexpected error occurred disabling 2FA for user with ID '{_userManager.GetUserId(User)}'.");
            }

            _logger.LogInformation("User with ID '{UserId}' has disabled 2fa.", _userManager.GetUserId(User));
            StatusMessage = "2fa has been disabled. You can reenable 2fa when you setup an authenticator app";
            return RedirectToAction("TwoFactorAuthentication");
        }
        public async Task<IActionResult> ResetAuthenticator()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            return View();
        }
        public async Task<IActionResult> ResetAuthenticatorOnPost()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _userManager.ResetAuthenticatorKeyAsync(user);
            _logger.LogInformation("User with ID '{UserId}' has reset their authentication app key.", user.Id);

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your authenticator app key has been reset, you will need to configure your authenticator app using the new key.";

            return RedirectToAction("EnableTwoFactorAuthenticator");
        }
        [HttpGet]
        public async Task<IActionResult> PhoneNumber_()
        {
            var phone = new PhoneNumberViewModel();
            var user = await _userManager.GetUserAsync(HttpContext.User);
            phone.PhoneNumber = await _userManager.GetPhoneNumberAsync(user);
            return View("PhoneNumber", phone);
        }
        [HttpPost]
        public async Task<IActionResult> AddPhoneNumber(PhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // Generate the token and send it
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var code = await _userManager.GenerateChangePhoneNumberTokenAsync(user, model.PhoneNumber);
            await _emailSender.SendSmsAsync(model.PhoneNumber, "Mã xác thực là: " + code);
            return RedirectToAction(nameof(VerifyPhoneNumber), new { PhoneNumber = model.PhoneNumber });
        }
        [HttpGet]
        public IActionResult VerifyPhoneNumber(string phoneNumber)
        {
            // var code = await _userManager.GenerateChangePhoneNumberTokenAsync(await _userManager.GetUserAsync(HttpContext.User), phoneNumber);
            // Send an SMS to verify the phone number
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Manage/VerifyPhoneNumber
        [HttpPost]
        public async Task<IActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user != null)
            {
                var result = await _userManager.ChangePhoneNumberAsync(user, model.PhoneNumber, model.Code);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    StatusMessage = "Xác thực số điện thoại thành công";
                    return RedirectToAction("Index");
                }
            }
            // If we got this far, something failed, redisplay the form
            ModelState.AddModelError(string.Empty, "Lỗi thêm số điện thoại");
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> RemovePhoneNumber()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user != null)
            {
                var result = await _userManager.SetPhoneNumberAsync(user, null);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    StatusMessage = "Xóa số điện thoại thành công";
                    return RedirectToAction(nameof(Index));
                }
            }
            StatusMessage = "Lỗi Xóa số điện thoại thất bại";
            return RedirectToAction(nameof(Index));
        }
    }
}