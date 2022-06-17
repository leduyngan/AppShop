using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace App.Areas.Identity.Models.ManageViewNodels
{
    public class VerifyPhoneNumberViewModel
    {
        [Required(ErrorMessage = "Phải nhập {0}")]
        [Display(Name = "Mã xác nhận")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Phải nhập {0}")]
        [Phone]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }
    }
}