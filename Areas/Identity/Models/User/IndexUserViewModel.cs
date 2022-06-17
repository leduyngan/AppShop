using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using App.Models;
using Microsoft.AspNetCore.Mvc;

namespace App.Areas.Identity.UserViewModel
{
    public class IndexUserViewModel
    {
        public class UserAndRole : AppUser
        {
            public string RoleNames { get; set; }
        }
        public List<UserAndRole> users { set; get; }

        public int Item_Per_Page = 10;

        //[BindProperty(SupportsGet = true, Name = "p")]
        public int currentPage { get; set; }
        public int countPage { get; set; }
        public int totalUsers { get; set; }
    }

}