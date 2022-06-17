using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Areas.Identity.UserViewModel
{
    public class AddUserRoleViewModel
    {
        public AppUser user { set; get; }
        [BindProperty]
        [DisplayName("Các role gán cho user")]
        public string[] RoleNames { set; get; }
        public SelectList allRoles { get; set; }
        public List<IdentityRoleClaim<string>> claimsInRole { set; get; }
        public List<IdentityUserClaim<string>> claimsInUserClaim { get; set; }

    }

}