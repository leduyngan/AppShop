using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App.Models;
using App.Models.Contacts;
using Microsoft.AspNetCore.Authorization;
using App.Data;

namespace App.Areas.Contact.Controllers
{
    [Area("Contact")]
    [Authorize(Roles = RoleName.Administrator)]
    public class ContactController : Controller
    {
        private readonly AppDbContext _context;

        public ContactController(AppDbContext context)
        {
            _context = context;
        }
        [TempData]
        public string StatusMessage { set; get; }

        [HttpGet("/admin/contact")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Contacts.ToListAsync());
        }

        [HttpGet("/admin/contact/detail/{id}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contactModel = await _context.Contacts
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contactModel == null)
            {
                return NotFound();
            }

            return View(contactModel);
        }

        [HttpGet("/contact/")]
        [AllowAnonymous]
        public IActionResult SendContact()
        {
            return View();
        }

        // POST: Contact/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost("/contact/")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendContact([Bind("FullName,Email,Message,Phone")] ContactModel contactModel)
        {
            if (ModelState.IsValid)
            {
                contactModel.DateSent = DateTime.Now;
                _context.Add(contactModel);
                await _context.SaveChangesAsync();
                StatusMessage = "Liên hệ của bạn đã được gửi";
                return RedirectToAction("Index", "Home");
            }
            return View(contactModel);
        }


        [HttpGet("/admin/contact/delete/{id}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contactModel = await _context.Contacts
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contactModel == null)
            {
                return NotFound();
            }

            return View(contactModel);
        }

        // POST: Contact/Delete/5
        [HttpPost("/admin/contact/delete/{id}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contactModel = await _context.Contacts.FindAsync(id);
            _context.Contacts.Remove(contactModel);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
