
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace App.Areas.Identity.RoleViewModels
{
    public class EditRoleViewModel
    {
        [DisplayName("Tên của role")]
        [Required(ErrorMessage = "Phải nhập {0}")]
        [StringLength(256, MinimumLength = 3, ErrorMessage = "{0} phải dài từ {2} đến {1} ký tự")]
        public string Name { get; set; }
        public List<IdentityRoleClaim<string>> Claims { get; set; }
        public IdentityRole Role { set; get; }
    }
}