using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace App.Areas.Identity.Models.ManageViewNodels
{
    public class GenerateRecoveryCodesViewModel
    {
        [TempData]
        public string[] RecoveryCodes { get; set; }
    }
}