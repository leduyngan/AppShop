using System.ComponentModel.DataAnnotations;

namespace App.Areas.Identity.Models.AccountViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Phải nhập {0}")]
        [Display(Name = "Địa chỉ Email hoặc tên tài khoản", Prompt = ("Địa chỉ email hoặc username"))]
        public string UserNameOrEmail { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu", Prompt = "Mật khẩu")]
        public string Password { get; set; }

        [Display(Name = "Nhớ thông tin đăng nhập?")]
        public bool RememberMe { get; set; }
    }
}