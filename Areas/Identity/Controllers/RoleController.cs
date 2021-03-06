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
                StatusMessage = $"B???n v???a t???o role m???i: {model.Name}";
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
            if (roleid == null) return NotFound("Kh??ng t??m th???y role");
            IdentityRole role = await _roleManager.FindByIdAsync(roleid);
            if (role == null)
            {
                return NotFound("Kh??ng t??m th???y role");
            }
            return View(role);
        }
        [HttpPost]
        public async Task<IActionResult> Delete(IdentityRole role)
        {
            if (role.Id == null) return NotFound("Kh??ng t??m th???y role");
            IdentityRole _role = await _roleManager.FindByIdAsync(role.Id);
            if (_role == null)
            {
                return NotFound("Kh??ng t??m th???y role");
            }

            var result = await _roleManager.DeleteAsync(_role);
            if (result.Succeeded)
            {
                StatusMessage = $"B???n v???a x??a: {_role.Name}";
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
            if (roleid == null) return NotFound("Kh??ng t??m th???y role");
            EditRoleViewModel role = new EditRoleViewModel();
            role.Role = await _roleManager.FindByIdAsync(roleid);

            if (role != null)
            {
                role.Name = role.Role.Name;
                role.Claims = await _conext.RoleClaims.Where(rc => rc.RoleId == role.Role.Id).ToListAsync();
                return View(role);
            }
            return NotFound("Kh??ng t??m th???y role");
        }
        [HttpPost("{roleid}"), ActionName("Edit")]
        public async Task<IActionResult> Edit(string roleid, [Bind("Name")] EditRoleViewModel
        model)
        {
            if (roleid == null) return NotFound("Kh??ng t??m th???y role");
            EditRoleViewModel role = new EditRoleViewModel();
            role.Role = await _roleManager.FindByIdAsync(roleid);
            if (role.Role == null)
            {
                return NotFound("Kh??ng t??m th???y role");
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
                StatusMessage = $"B???n v???a ?????i t??n: {role.Role.Name}";
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
                return NotFound("Kh??ng t??m th???y role");
            }
            return View(role);
        }
        [HttpPost]
        public async Task<IActionResult> RoleClaimAdd(string roleid, EditRoleClaimViewModel model)
        {
            var role = await _roleManager.FindByIdAsync(roleid);
            if (role == null)
            {
                return NotFound("Kh??ng t??m th???y role");
            }
            if (!ModelState.IsValid)
            {
                return View();
            }
            if ((await _roleManager.GetClaimsAsync(role)).Any(c => c.Type == model.ClaimType && c.Value == model.ClaimValue))
            {
                ModelState.AddModelError(string.Empty, "Claim n??y ???? c?? trong role");
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

            StatusMessage = "V???a th??m ?????c t??nh (Claim) m???i";

            return RedirectToAction("Edit", new { roleid = role.Id });
        }
        [HttpGet]
        public async Task<IActionResult> RoleClaimEdit(int? claimid)
        {
            if (claimid == null)
            {
                return NotFound("Kh??ng t??m th???y claim");
            }
            ViewBag.claimid = claimid;
            var claim = _conext.RoleClaims.Where(c => c.Id == claimid).FirstOrDefault();
            if (claim == null) return NotFound("Kh??ng t??m th???y claim");

            var role = await _roleManager.FindByIdAsync(claim.RoleId);
            if (role == null)
            {
                return NotFound("Kh??ng t??m th???y role");
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
                return NotFound("Kh??ng t??m th???y claim");
            }
            var claim = _conext.RoleClaims.Where(c => c.Id == claimid).FirstOrDefault();
            if (claim == null) return NotFound("Kh??ng t??m th???y claim");

            var role = await _roleManager.FindByIdAsync(claim.RoleId);
            if (role == null)
            {
                return NotFound("Kh??ng t??m th???y role");
            }
            if (!ModelState.IsValid)
            {
                return View();
            }
            if (_conext.RoleClaims.Any(c => c.RoleId == role.Id && c.ClaimType == model.ClaimType && c.ClaimValue == model.ClaimValue && c.Id != claimid))
            {
                ModelState.AddModelError(string.Empty, "Claim n??y ???? c?? trong role");
                return View();
            }

            claim.ClaimType = model.ClaimType;
            claim.ClaimValue = model.ClaimValue;
            await _conext.SaveChangesAsync();

            StatusMessage = "V???a c???p nh???t claim";

            return RedirectToAction("Edit", new { roleid = role.Id });
        }
        [HttpPost]
        public async Task<IActionResult> RoleClaimDelete(int? claimid)
        {
            if (claimid == null)
            {
                return NotFound("Kh??ng t??m th???y claim");
            }
            var claim = _conext.RoleClaims.Where(c => c.Id == claimid).FirstOrDefault();
            if (claim == null) return NotFound("Kh??ng t??m th???y claim");

            var role = await _roleManager.FindByIdAsync(claim.RoleId);
            if (role == null)
            {
                return NotFound("Kh??ng t??m th???y role");
            }

            await _roleManager.RemoveClaimAsync(role, new Claim(claim.ClaimType, claim.ClaimValue));
            // _conext.RoleClaims.Remove(claim);
            // await _conext.SaveChangesAsync();
            StatusMessage = "V???a x??a claim";

            return RedirectToAction("Edit", new { roleid = role.Id });
        }

    }
}