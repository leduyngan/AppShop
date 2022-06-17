using Microsoft.AspNetCore.Identity;

namespace App.Areas.Identity.RoleViewModels
{
    public class IndexRoleViewModel : IdentityRole
    {
        public string[] Claims { get; set; }
    }
}