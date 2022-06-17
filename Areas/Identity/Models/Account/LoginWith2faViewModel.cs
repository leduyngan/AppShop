using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Areas.Identity.Models.AccountViewModels
{
    public class LoginWith2faSendCodeViewModel
    {
        public string SelectedProvider { get; set; }

        public ICollection<SelectListItem> Providers { get; set; }

        public string ReturnUrl { get; set; }

        public bool RememberMe { get; set; }
    }
}