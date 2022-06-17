using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace App.Areas.Identity.Models.ManageViewNodels
{
    public class EnableTwoFactorAuthenticatorViewModel
    {
        public string SharedKey { get; set; }

        public string AuthenticatorUri { get; set; }
        public string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        [TempData]
        public string[] RecoveryCodes { get; set; }
        [Required]
        [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Verification Code")]
        public string Code { get; set; }
    }
}