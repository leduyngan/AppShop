using System.ComponentModel.DataAnnotations;

namespace App.Areas.Identity.Models.AccountViewModels
{
    public class ResetPassworViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Nhập lại mật khẩu mới")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Lặt lại mật khẩu")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }
}