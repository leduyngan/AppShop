using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using App.Areas.Identity.RoleViewModels;
using App.Data;
using App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace App.Areas.Identity.Controllers
{
    [Authorize(Roles = RoleName.Administrator)]
    [Area("Identity")]
    [Route("Role/[action]")]
    public class RoleController : Controller
    {
        protected readonly RoleManager<IdentityRole> _roleManager;
        protected readonly AppDbContext _conext;
        public RoleController(RoleManager<IdentityRole> roleManager, AppDbContext conext)
        {
            _roleManager = roleManager;
            _conext = conext;
        }
        [TempData]
        public string StatusMessage { get; set; }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var r = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            var roles = new List<IndexRoleViewModel>();
            foreach (var _r in r)
            {
                var claims = await _roleManager.GetClaimsAsync(_r);
                var claimString = claims.Select(c => c.Type + ": " + c.Value);
                var rm = new IndexRoleViewModel()
                {
                    Name = _r.Name,
                    Id = _r.Id,
                    Claims = claimString.ToArray()
                };
                roles.Add(rm);
            }
            return View(roles);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(CreateRoleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }
            var newRole = new IdentityRole(model.Name);
            var result = await _roleManager.CreateAsync(newRole);
            if (result.Succeeded)
            {
                StatusMessage = $"Bạn vừa tạo role mới: {model.Name}";
                return RedirectToAction("Index");
            }
            else
            {
                result.Errors.ToList().ForEach(error =>
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                });
            }
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Delete(string roleid)
        {
            if (roleid == null) return NotFound("Không tìm thấy role");
            IdentityRole role = await _roleManager.FindByIdAsync(roleid);
            if (role == null)
            {
                return NotFound("Không tìm thấy role");
            }
            return View(role);
        }
        [HttpPost]
        public async Task<IActionResult> Delete(IdentityRole role)
        {
            if (role.Id == null) return NotFound("Không tìm thấy role");
            IdentityRole _role = await _roleManager.FindByIdAsync(role.Id);
            if (_role == null)
            {
                return NotFound("Không tìm thấy role");
            }

            var result = await _roleManager.DeleteAsync(_role);
            if (result.Succeeded)
            {
                StatusMessage = $"Bạn vừa xóa: {_role.Name}";
                return RedirectToAction("Index");
            }
            else
            {
                result.Errors.ToList().ForEach(error =>
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                });
            }
            return View();
        }
        [HttpGet("{roleid}")]
        public async Task<IActionResult> Edit(string roleid)
        {
            if (roleid == null) return NotFound("Không tìm thấy role");
            EditRoleViewModel role = new EditRoleViewModel();
            role.Role = await _roleManager.FindByIdAsync(roleid);

            if (role != null)
            {
                role.Name = role.Role.Name;
                role.Claims = await _conext.RoleClaims.Where(rc => rc.RoleId == role.Role.Id).ToListAsync();
                return View(role);
            }
            return NotFound("Không tìm thấy role");
        }
        [HttpPost("{roleid}"), ActionName("Edit")]
        public async Task<IActionResult> Edit(string roleid, [Bind("Name")] EditRoleViewModel
        model)
        {
            if (roleid == null) return NotFound("Không tìm thấy role");
            EditRoleViewModel role = new EditRoleViewModel();
            role.Role = await _roleManager.FindByIdAsync(roleid);
            if (role.Role == null)
            {
                return NotFound("Không tìm thấy role");
            }
            role.Claims = await _conext.RoleClaims.Where(rc => rc.RoleId == roleid).ToListAsync();
            if (!ModelState.IsValid)
            {
                return View();
            }
            role.Role.Name = model.Name;
            var result = await _roleManager.UpdateAsync(role.Role);
            if (result.Succeeded)
            {
                StatusMessage = $"Bạn vừa đổi tên: {role.Role.Name}";
                return RedirectToAction("Index");
            }
            else
            {
                result.Errors.ToList().ForEach(error =>
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                });
            }
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> RoleClaimAdd(string roleid)
        {
            EditRoleClaimViewModel role = new EditRoleClaimViewModel();
            role.Role = await _roleManager.FindByIdAsync(roleid);
            if (role.Role == null)
            {
                return NotFound("Không tìm thấy role");
            }
            return View(role);
        }
        [HttpPost]
        public async Task<IActionResult> RoleClaimAdd(string roleid, EditRoleClaimViewModel model)
        {
            var role = await _roleManager.FindByIdAsync(roleid);
            if (role == null)
            {
                return NotFound("Không tìm thấy role");
            }
            if (!ModelState.IsValid)
            {
                return View();
            }
            if ((await _roleManager.GetClaimsAsync(role)).Any(c => c.Type == model.ClaimType && c.Value == model.ClaimValue))
            {
                ModelState.AddModelError(string.Empty, "Claim này đã có trong role");
                return View();
            }
            var newClaim = new Claim(model.ClaimType, model.ClaimValue);
            var result = await _roleManager.AddClaimAsync(role, newClaim);

            if (!result.Succeeded)
            {
                result.Errors.ToList().ForEach(e =>
                {
                    ModelState.AddModelError(string.Empty, e.Description);
                });
                return View();
            }

            StatusMessage = "Vừa thêm đặc tính (Claim) mới";

            return RedirectToAction("Edit", new { roleid = role.Id });
        }
        [HttpGet]
        public async Task<IActionResult> RoleClaimEdit(int? claimid)
        {
            if (claimid == null)
            {
                return NotFound("Không tìm thấy claim");
            }
            ViewBag.claimid = claimid;
            var claim = _conext.RoleClaims.Where(c => c.Id == claimid).FirstOrDefault();
            if (claim == null) return NotFound("Không tìm thấy claim");

            var role = await _roleManager.FindByIdAsync(claim.RoleId);
            if (role == null)
            {
                return NotFound("Không tìm thấy role");
            }
            EditRoleClaimViewModel RoleClaim = new EditRoleClaimViewModel()
            {
                ClaimType = claim.ClaimType,
                ClaimValue = claim.ClaimValue,
                Role = role
            };
            return View(RoleClaim);
        }
        [HttpPost]
        public async Task<IActionResult> RoleClaimEdit(int? claimid, EditRoleClaimViewModel model)
        {
            if (claimid == null)
            {
                return NotFound("Không tìm thấy claim");
            }
            var claim = _conext.RoleClaims.Where(c => c.Id == claimid).FirstOrDefault();
            if (claim == null) return NotFound("Không tìm thấy claim");

            var role = await _roleManager.FindByIdAsync(claim.RoleId);
            if (role == null)
            {
                return NotFound("Không tìm thấy role");
            }
            if (!ModelState.IsValid)
            {
                return View();
            }
            if (_conext.RoleClaims.Any(c => c.RoleId == role.Id && c.ClaimType == model.ClaimType && c.ClaimValue == model.ClaimValue && c.Id != claimid))
            {
                ModelState.AddModelError(string.Empty, "Claim này đã có trong role");
                return View();
            }

            claim.ClaimType = model.ClaimType;
            claim.ClaimValue = model.ClaimValue;
            await _conext.SaveChangesAsync();

            StatusMessage = "Vừa cập nhật claim";

            return RedirectToAction("Edit", new { roleid = role.Id });
        }
        [HttpPost]
        public async Task<IActionResult> RoleClaimDelete(int? claimid)
        {
            if (claimid == null)
            {
                return NotFound("Không tìm thấy claim");
            }
            var claim = _conext.RoleClaims.Where(c => c.Id == claimid).FirstOrDefault();
            if (claim == null) return NotFound("Không tìm thấy claim");

            var role = await _roleManager.FindByIdAsync(claim.RoleId);
            if (role == null)
            {
                return NotFound("Không tìm thấy role");
            }

            await _roleManager.RemoveClaimAsync(role, new Claim(claim.ClaimType, claim.ClaimValue));
            // _conext.RoleClaims.Remove(claim);
            // await _conext.SaveChangesAsync();
            StatusMessage = "Vừa xóa claim";

            return RedirectToAction("Edit", new { roleid = role.Id });
        }

    }
}