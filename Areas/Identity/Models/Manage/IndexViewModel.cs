using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace App.Areas.Identity.Models.ManageViewNodels
{
    public class IndexViewModel
    {
        public EditExtraProfileModel profile { get; set; }
        public bool HasPassword { get; set; }

        public IList<UserLoginInfo> Logins { get; set; }

        public string PhoneNumber { get; set; }

        public bool TwoFactor { get; set; }

        public bool BrowserRemembered { get; set; }

        public string AuthenticatorKey { get; set; }
    }
}