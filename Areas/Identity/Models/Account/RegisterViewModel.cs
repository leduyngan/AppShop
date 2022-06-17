using System.ComponentModel.DataAnnotations;

namespace App.Areas.Identity.Models.AccountViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Phải nhập {0}")]
        [EmailAddress(ErrorMessage = "Sai định dạng Email")]
        [Display(Name = "Email", Prompt = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "{0} phải dài từ {2} đến {1} ký tự.", MinimumLength = 3)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu", Prompt = "Mật khẩu")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Lặp lại mật khẩu", Prompt = "Lặp lại mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu lạp lại không chính xác.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Phải nhập {0}")]
        [Display(Name = "Tên tài khoản", Prompt = "Tên tài khoản")]
        [StringLength(100, ErrorMessage = "{0} phải dài từ {2} đến {1} ký tự.", MinimumLength = 3)]
        [DataType(DataType.Text)]
        public string UserName { get; set; }
    }
}