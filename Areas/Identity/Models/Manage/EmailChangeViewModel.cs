using System;
using System.ComponentModel.DataAnnotations;

namespace App.Areas.Identity.Models.ManageViewNodels
{
    public class EmailChangeViewModel
    {

        [EmailAddress]
        [Display(Name = "Email hiện tại")]
        public string Email { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email mới")]
        public string NewEmail { get; set; }

        [Display(Name = "Xác thực Email")]
        public bool EmailIsConfirmed { get; set; }
    }
}