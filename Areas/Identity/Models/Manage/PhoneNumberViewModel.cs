using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace App.Areas.Identity.Models.ManageViewNodels
{
    public class PhoneNumberViewModel
    {
        [Required]
        [Phone]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }
    }
}
