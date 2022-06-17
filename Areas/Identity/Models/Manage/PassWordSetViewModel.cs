using System.ComponentModel.DataAnnotations;

namespace App.Areas.Identity.Models.ManageViewNodels
{
    public class PassWordSetViewModel
    {
        [Required]
        [StringLength(100, ErrorMessage = "{0} phải dài từ {2} đến {1} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Nhập lại mật khẩu")]
        [Compare("NewPassword", ErrorMessage = "Lặp lại mật khẩu không chính xác.")]
        public string ConfirmPassword { get; set; }
    }
}