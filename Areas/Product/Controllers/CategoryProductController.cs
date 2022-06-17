using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App.Models;
using App.Models.Product;
using Microsoft.AspNetCore.Authorization;
using App.Data;

namespace App.Areas.Product.Controllers
{
    [Route("admin/categoryproduct/category/[action]/{id?}")]
    [Authorize(Roles = RoleName.Administrator)]
    [Area("Product")]
    public class CategoryProductController : Controller
    {
        private readonly AppDbContext _context;

        public CategoryProductController(AppDbContext context)
        {
            _context = context;
        }
        [TempData]
        public string StatusMessage { set; get; }
        // GET: Category
        public async Task<IActionResult> Index()
        {
            var qr = (from c in _context.CategoryProducts select c)
                     .Include(c => c.ParentCategory)
                     .Include(c => c.CategoryChildren);
            var categoryProduct = (await qr.ToListAsync())
                            .Where(c => c.ParentCategory == null)
                            .ToList();
            return View(categoryProduct);
        }

        // GET: Category/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.CategoryProducts
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }
        private void CreateSelectItems(List<CategoryProduct> source, List<CategoryProduct> des, int level)
        {
            string prefix = string.Concat(Enumerable.Repeat("----", level));
            foreach (var category in source)
            {
                //category.Title = prefix + " " + category.Title;

                des.Add(new CategoryProduct()
                {
                    Id = category.Id,
                    Title = prefix + " " + category.Title
                });
                if (category.CategoryChildren?.Count > 0)
                {
                    CreateSelectItems(category.CategoryChildren.ToList(), des, level + 1);
                }
            }
        }
        // GET: Category/Create
        public async Task<IActionResult> Create()
        {
            var qr = (from c in _context.CategoryProducts select c)
                     .Include(c => c.ParentCategory)
                     .Include(c => c.CategoryChildren);
            var categories = (await qr.ToListAsync())
                            .Where(c => c.ParentCategory == null)
                            .ToList();
            categories.Insert(0, new CategoryProduct()
            {
                Id = -1,
                Title = "Không có danh mục cha"
            });
            var items = new List<CategoryProduct>();
            CreateSelectItems(categories, items, 0);
            var selectList = new SelectList(items, "Id", "Title");
            ViewData["ParentCategoryId"] = selectList;
            return View();
        }

        // POST: Category/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Slug,ParentCategoryId")] CategoryProduct category)
        {
            if (ModelState.IsValid)
            {
                if (category.ParentCategoryId == -1) category.ParentCategoryId = null;
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            var qr = (from c in _context.CategoryProducts select c)
                     .Include(c => c.ParentCategory)
                     .Include(c => c.CategoryChildren);
            var categories = (await qr.ToListAsync())
                            .Where(c => c.ParentCategory == null)
                            .ToList();
            categories.Insert(0, new CategoryProduct()
            {
                Id = -1,
                Title = "Không có danh mục cha"
            });
            var items = new List<CategoryProduct>();
            CreateSelectItems(categories, items, 0);
            var selectList = new SelectList(items, "Id", "Title");
            ViewData["ParentCategoryId"] = selectList;
            return View(category);
        }

        // GET: Category/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.CategoryProducts.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            var qr = (from c in _context.CategoryProducts select c)
                     .Include(c => c.ParentCategory)
                     .Include(c => c.CategoryChildren);
            var categories = (await qr.ToListAsync())
                            .Where(c => c.ParentCategory == null)
                            .ToList();
            categories.Insert(0, new CategoryProduct()
            {
                Id = -1,
                Title = "Không có danh mục cha"
            });
            var items = new List<CategoryProduct>();
            CreateSelectItems(categories, items, 0);
            var selectList = new SelectList(items, "Id", "Title");
            ViewData["ParentCategoryId"] = selectList;
            return View(category);
        }

        // POST: Category/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Slug,ParentCategoryId")] CategoryProduct category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }
            if (category.ParentCategoryId == category.Id)
            {
                ModelState.AddModelError(string.Empty, "Phải chọn danh mục cha khác");
            }

            // kiem tra thiet lap muc tra phu hop
            bool canUpdate = true;
            if (canUpdate && category.ParentCategoryId != null)
            {
                var childCates = (from c in _context.CategoryProducts select c)
                                .Include(c => c.CategoryChildren)
                                .ToList()
                                .Where(c => c.ParentCategoryId == category.Id);


                // func check Id
                Func<List<CategoryProduct>, bool> checkCateIds = null;
                checkCateIds = (cates) =>
                {
                    foreach (var cate in cates)
                    {
                        Console.WriteLine(cate.Title);
                        if (cate.Id == category.ParentCategoryId)
                        {
                            canUpdate = false;
                            ModelState.AddModelError(string.Empty, "Phải chọn danh mục cha khác");
                            return true;
                        }
                        if (cate.CategoryChildren != null)
                        {
                            return checkCateIds(cate.CategoryChildren.ToList());
                        }
                    }
                    return false;

                };
                // end func
                checkCateIds(childCates.ToList());
            }

            if (ModelState.IsValid && canUpdate)
            {
                try
                {
                    if (category.ParentCategoryId == -1)
                        category.ParentCategoryId = null;
                    // _context.Update(category);
                    var dtc = _context.CategoryProducts.FirstOrDefault(c => c.Id == id);
                    if (dtc == null)
                    {
                        return NotFound();
                    }
                    dtc.ParentCategoryId = category.ParentCategoryId;
                    dtc.Title = category.Title;
                    dtc.Description = category.Description;
                    dtc.Slug = dtc.Slug;

                    _context.Update(dtc);
                    // _context.Entry(dtc).State = EntityState.Detached;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                StatusMessage = "Vừa cập nhật chuyên mục";
                return RedirectToAction(nameof(Index));
            }
            var qr = (from c in _context.CategoryProducts select c)
                        .Include(c => c.ParentCategory)
                        .Include(c => c.CategoryChildren);
            var categories = (await qr.ToListAsync())
                            .Where(c => c.ParentCategory == null)
                            .ToList();
            categories.Insert(0, new CategoryProduct()
            {
                Id = -1,
                Title = "Không có danh mục cha"
            });
            var items = new List<CategoryProduct>();
            CreateSelectItems(categories, items, 0);
            var selectList = new SelectList(items, "Id", "Title");
            ViewData["ParentCategoryId"] = selectList;
            return View(category);
        }

        // GET: Category/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.CategoryProducts
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.CategoryProducts
                            .Include(c => c.CategoryChildren)
                            .FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }
            foreach (var cCategory in category.CategoryChildren)
            {
                cCategory.ParentCategoryId = category.ParentCategoryId;
            }


            _context.CategoryProducts.Remove(category);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.CategoryProducts.Any(e => e.Id == id);
        }
    }
}
