using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Areas.Identity.UserViewModel
{
    public class UserClaimViewModel
    {
        [DisplayName("Kiểu (tên) claim")]
        [Required(ErrorMessage = "Phải nhập {0}")]
        [StringLength(256, MinimumLength = 3, ErrorMessage = "{0} phải dài từ {2} đến {1} ký tự")]
        public string ClaimType { get; set; }
        [DisplayName("Giá trị")]
        [Required(ErrorMessage = "Phải nhập {0}")]
        [StringLength(256, MinimumLength = 3, ErrorMessage = "{0} phải dài từ {2} đến {1} ký tự")]
        public string ClaimValue { get; set; }
        public AppUser user { set; get; }
        public IdentityUserClaim<string> userClaim { get; set; }

    }

}