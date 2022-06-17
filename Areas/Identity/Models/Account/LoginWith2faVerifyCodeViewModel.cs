using System.ComponentModel.DataAnnotations;

namespace App.Areas.Identity.Models.AccountViewModels
{
    public class LoginWith2faVerifyCodeViewModel
    {
        [Required]
        public string Provider { get; set; }

        [Required]
        public string Code { get; set; }

        public string ReturnUrl { get; set; }

        [Display(Name = "Nhớ cho trình duyệt này?")]
        public bool RememberBrowser { get; set; }

        [Display(Name = "Nhớ thông tin đăng nhập?")]
        public bool RememberMe { get; set; }
    }
}