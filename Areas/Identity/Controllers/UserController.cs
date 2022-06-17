using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using App.Areas.Identity.UserViewModel;
using App.Data;
using App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using static App.Areas.Identity.UserViewModel.IndexUserViewModel;

namespace App.Areas.Identity.Controllers
{

    [Authorize(Roles = RoleName.Administrator)]
    [Area("Identity")]
    [Route("User/[action]")]
    public class UserController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        protected readonly AppDbContext _context;
        public UserController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, RoleManager<IdentityRole> roleManager, AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
        }
        [TempData]
        public string StatusMessage { get; set; }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var Ursers = new IndexUserViewModel();
            Ursers.totalUsers = await _userManager.Users.CountAsync();
            Ursers.countPage = (int)Math.Ceiling((double)Ursers.totalUsers / Ursers.Item_Per_Page);

            if (Ursers.currentPage < 1)
                Ursers.currentPage = 1;
            if (Ursers.currentPage > Ursers.countPage)
                Ursers.currentPage = Ursers.countPage;

            var qr1 = _userManager.Users.OrderBy(u => u.UserName)
                    .Skip((Ursers.currentPage - 1) * Ursers.Item_Per_Page)
                    .Take(Ursers.Item_Per_Page)
                    .Select(u => new UserAndRole()
                    {
                        Id = u.Id,
                        UserName = u.UserName,
                    });
            Ursers.users = await qr1.ToListAsync();
            foreach (var user in Ursers.users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                user.RoleNames = string.Join(",", roles);
            }
            return View(Ursers);

        }
        [HttpGet]
        public async Task<IActionResult> UserSetPassword(string id)
        {
            var Users = new UserSetpasswordViewModel();
            if (string.IsNullOrEmpty(id))
            {
                return NotFound($"Không có user");
            }
            Users.user = await _userManager.FindByIdAsync(id);
            if (Users.user == null)
            {
                return NotFound($"Không tìm thất user, id = {id}");
            }
            return View(Users);
        }
        public async Task<IActionResult> UserSetPassword(string id, UserSetpasswordViewModel model)
        {
            var Users = new UserSetpasswordViewModel();
            if (string.IsNullOrEmpty(id))
            {
                return NotFound($"Không có user");
            }

            Users.user = await _userManager.FindByIdAsync(id);

            if (Users.user == null)
            {
                return NotFound($"Không tìm thất user, id = {id}");
            }
            if (!ModelState.IsValid)
            {
                return View();
            }

            await _userManager.RemovePasswordAsync(Users.user);
            var addPasswordResult = await _userManager.AddPasswordAsync(Users.user, model.NewPassword);
            if (!addPasswordResult.Succeeded)
            {
                foreach (var error in addPasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View();
            }
            StatusMessage = $"Vừa cập nhật mật khẩu cho user: {Users.user.UserName}";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> AddUserRole(string id)
        {
            var Users = new AddUserRoleViewModel();
            if (string.IsNullOrEmpty(id))
            {
                return NotFound($"Không có user");
            }
            Users.user = await _userManager.FindByIdAsync(id);
            if (Users.user == null)
            {
                return NotFound($"Không tìm thấy user, id = {id}");
            }
            Users.RoleNames = (await _userManager.GetRolesAsync(Users.user)).ToArray<string>();

            List<string> roleNames = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            Users.allRoles = new SelectList(roleNames);
            var claims = await GetClaims(id);
            Users.claimsInRole = claims.claimsInRole;
            Users.claimsInUserClaim = claims.claimsInUserClaim;
            return View(Users);
        }
        [HttpPost]
        public async Task<IActionResult> AddUserRole(string id, AddUserRoleViewModel model)
        {
            var Users = new AddUserRoleViewModel();
            if (string.IsNullOrEmpty(id))
            {
                return NotFound($"Không có user");
            }

            Users.user = await _userManager.FindByIdAsync(id);

            if (Users.user == null)
            {
                return NotFound($"Không tìm thất user, id = {id}");
            }
            await GetClaims(id);
            var OldRoleNames = (await _userManager.GetRolesAsync(Users.user)).ToArray();
            var deleteRoles = OldRoleNames.Where(r => !model.RoleNames.Contains(r));
            var addRoles = model.RoleNames.Where(r => !OldRoleNames.Contains(r));

            List<string> roleNames = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            Users.allRoles = new SelectList(roleNames);

            var resultDelete = await _userManager.RemoveFromRolesAsync(Users.user, deleteRoles);
            if (!resultDelete.Succeeded)
            {
                resultDelete.Errors.ToList().ForEach(error =>
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                });
                return View(Users);
            }

            var resultAdd = await _userManager.AddToRolesAsync(Users.user, addRoles);
            if (!resultAdd.Succeeded)
            {
                resultAdd.Errors.ToList().ForEach(error =>
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                });
                return View(Users);
            }

            StatusMessage = $"Vừa cập nhật role cho user: {Users.user.UserName}";

            return RedirectToAction("Index");
        }
        async Task<AddUserRoleViewModel> GetClaims(string id)
        {
            var UserRole = new AddUserRoleViewModel();
            var listRoles = from r in _context.Roles
                            join ur in _context.UserRoles on r.Id equals ur.RoleId
                            where ur.UserId == id
                            select r;
            var _claimInRole = from c in listRoles
                               join rc in _context.RoleClaims on c.Id equals rc.RoleId
                               select rc;
            UserRole.claimsInRole = await _claimInRole.ToListAsync();


            UserRole.claimsInUserClaim = await (from c in _context.UserClaims
                                                where c.UserId == id
                                                select c).ToListAsync();
            return UserRole;
        }
        [HttpGet]
        public async Task<IActionResult> AddUserClaim(string userid)
        {
            var UserClaims = new UserClaimViewModel();
            UserClaims.user = await _userManager.FindByIdAsync(userid);
            if (UserClaims.user == null) return NotFound("Không tìm thấy user");
            return View("UserClaim", UserClaims);
        }
        [HttpPost]
        public async Task<IActionResult> AddUserClaim(string userid, UserClaimViewModel model)
        {
            var UserClaims = new UserClaimViewModel();
            UserClaims.user = await _userManager.FindByIdAsync(userid);
            if (UserClaims.user == null) return NotFound("Không tìm thấy user");
            if (!ModelState.IsValid) return View("UserClaim");
            var claims = _context.UserClaims.Where(c => c.UserId == userid);
            if (claims.Any(c => c.ClaimType == model.ClaimType && c.ClaimValue == model.ClaimValue))
            {
                ModelState.AddModelError(string.Empty, "Đặc tính này đã có");
                return View("UserClaim", UserClaims);
            }
            await _userManager.AddClaimAsync(UserClaims.user, new Claim(model.ClaimType, model.ClaimValue));
            StatusMessage = "Đã thêm đặc tính cho user";
            return RedirectToAction("AddUserRole", new { id = userid });
        }
        [HttpGet]
        public async Task<IActionResult> EditUserClaim(int? claimid)
        {
            var UserClaims = new UserClaimViewModel();
            if (claimid == null) return NotFound("Không tìm thấy claim");
            UserClaims.userClaim = _context.UserClaims.Where(c => c.Id == claimid).FirstOrDefault();
            UserClaims.user = await _userManager.FindByIdAsync(UserClaims.userClaim.UserId);
            if (UserClaims.user == null) return NotFound("Không tìm thất user");
            UserClaims.ClaimType = UserClaims.userClaim.ClaimType;
            UserClaims.ClaimValue = UserClaims.userClaim.ClaimValue;
            return View("UserClaim", UserClaims);
        }
        [HttpPost]
        public async Task<IActionResult> EditUserClaim(int? claimid, UserClaimViewModel model)
        {
            var UserClaims = new UserClaimViewModel();
            if (claimid == null) return NotFound("Không tìm thấy claim");
            UserClaims.userClaim = _context.UserClaims.Where(c => c.Id == claimid).FirstOrDefault();
            UserClaims.user = await _userManager.FindByIdAsync(UserClaims.userClaim.UserId);
            if (UserClaims.user == null) return NotFound("Không tìm thất user");
            if (!ModelState.IsValid)
            {
                return View("UserClaim");
            }
            if (_context.UserClaims.Any(c => c.UserId == UserClaims.user.Id
            && c.ClaimType == model.ClaimType
            && c.ClaimValue == model.ClaimValue
            && c.Id != UserClaims.userClaim.Id))
            {
                ModelState.AddModelError(string.Empty, "Claim này đã có");
                return View("UserClaim");
            }
            UserClaims.userClaim.ClaimType = model.ClaimType;
            UserClaims.userClaim.ClaimValue = model.ClaimValue;
            await _context.SaveChangesAsync();
            StatusMessage = "Bạn vừa cập nhật claim";

            return RedirectToAction("AddUserRole", new { id = UserClaims.user.Id });
        }
        [HttpPost]
        public async Task<IActionResult> DeleteUserClaim(int? claimid)
        {
            var UserClaims = new UserClaimViewModel();
            if (claimid == null) return NotFound("Không tìm thấy claim");
            UserClaims.userClaim = _context.UserClaims.Where(c => c.Id == claimid).FirstOrDefault();
            UserClaims.user = await _userManager.FindByIdAsync(UserClaims.userClaim.UserId);
            if (UserClaims.user == null) return NotFound("Không tìm thấy user");

            await _userManager.RemoveClaimAsync(UserClaims.user, new Claim(UserClaims.userClaim.ClaimType, UserClaims.userClaim.ClaimValue));

            StatusMessage = "Bạn đã xóa claim";

            return RedirectToAction("AddUserRole", new { id = UserClaims.user.Id });
        }
    }
}
