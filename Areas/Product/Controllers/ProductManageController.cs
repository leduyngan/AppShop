using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App.Models;
using App.Models.Blog;
using Microsoft.AspNetCore.Authorization;
using App.Data;
using App.Helper;
using App.Areas.Blog.Models;
using Microsoft.AspNetCore.Identity;
using App.Utilities;
using App.Models.Product;
using App.Areas.Product.Models;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace App.Areas.Product.Controllers
{
    [Area("Product")]
    [Route("admin/product/productmanage/[action]/{id?}")]
    [Authorize(Roles = RoleName.Administrator + "," + RoleName.Editor)]
    public class ProductManageController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ProductManageController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [TempData]
        public string StatusMessage { set; get; }
        // GET: Post
        public async Task<IActionResult> Index([FromQuery(Name = "p")] int currentPage, int pageSize)
        {
            var products = _context.Products
                        .Include(p => p.Author)
                        .OrderByDescending(p => p.DateUpdated);


            int totalProducts = await products.CountAsync();
            if (pageSize <= 0) pageSize = 12;
            int countPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            if (currentPage > countPages) currentPage = countPages;
            if (currentPage < 1) currentPage = 1;
            var pagingModel = new PagingModel()
            {
                countpage = countPages,
                currentpage = currentPage,
                generateUrl = (pageNumber => Url.Action("Index", new
                {
                    p = pageNumber,
                    pageSize = pageSize
                }))
            };
            ViewBag.pagingModel = pagingModel;
            ViewBag.totalProducts = totalProducts;
            ViewBag.productIndex = (currentPage - 1) * pageSize;
            var productsInPage = await products.Skip((currentPage - 1) * pageSize)
                                    .Take(pageSize)
                                    .Include(p => p.ProductCategoryProducts)
                                    .ThenInclude(pc => pc.Category)
                                    .ToListAsync();
            return View(productsInPage);
        }

        // GET: Post/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Author)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Post/Create
        public async Task<IActionResult> Create()
        {
            var categories = await _context.CategoryProducts.ToListAsync();
            ViewData["categories"] = new MultiSelectList(categories, "Id", "Title");
            return View();
        }

        // POST: Post/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryIDs,Title,Description,Slug,Content,Published,Price")] CreateProductModel product)
        {
            var categories = await _context.CategoryProducts.ToListAsync();
            ViewData["categories"] = new MultiSelectList(categories, "Id", "Title");
            if (product.Slug == null)
            {
                product.Slug = AppUtilities.GenerateSlug(product.Title);
            }
            if (await _context.Products.AnyAsync(p => p.Slug == product.Slug))
            {
                ModelState.AddModelError("Slug", "Nhập chuối Url khác");
                return View(product);
            }

            if (ModelState.IsValid)
            {
                var _user = await _userManager.GetUserAsync(this.User);
                product.DateCreated = product.DateUpdated = DateTime.Now;
                product.AuthorId = _user.Id;
                _context.Add(product);

                if (product.CategoryIDs != null)
                {
                    foreach (var CateId in product.CategoryIDs)
                    {
                        _context.Add(new ProductCategoryProduct()
                        {
                            CategoryID = CateId,
                            Product = product
                        });
                    }
                }

                await _context.SaveChangesAsync();
                StatusMessage = "Vừa tạo bài viết mới";
                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        // GET: Post/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.Include(p => p.ProductCategoryProducts)
                                                .Where(p => p.ProductId == id)
                                                .FirstOrDefaultAsync();
            //var product = await _context.Products.Include(p => p.ProductCategoryProducts).FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }
            var productEdit = new CreateProductModel()
            {
                ProductId = product.ProductId,
                Title = product.Title,
                Price = product.Price,
                Content = product.Content,
                Description = product.Description,
                Slug = product.Slug,
                Published = product.Published,
                CategoryIDs = product.ProductCategoryProducts.Select(pc => pc.CategoryID).ToArray()
            };
            var categories = await _context.CategoryProducts.ToListAsync();
            ViewData["categories"] = new MultiSelectList(categories, "Id", "Title");
            return View(productEdit);
        }

        // POST: Post/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryIDs,ProductId,Title,Description,Slug,Content,Published,Price")] CreateProductModel product)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            if (product.Slug == null)
            {
                product.Slug = AppUtilities.GenerateSlug(product.Title);
            }
            if (await _context.Products.AnyAsync(p => p.Slug == product.Slug && p.ProductId != id))
            {
                ModelState.AddModelError("Slug", "Nhập chuối Url khác");
                return View(product);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var productUpdate = await _context.Products.Include(p => p.ProductCategoryProducts).FirstOrDefaultAsync(p => p.ProductId == id);
                    if (productUpdate == null)
                    {
                        return NotFound();
                    }

                    productUpdate.Title = product.Title;
                    productUpdate.Price = product.Price;
                    productUpdate.Description = product.Description;
                    productUpdate.Content = product.Content;
                    productUpdate.Published = product.Published;
                    productUpdate.Slug = product.Slug;
                    productUpdate.DateUpdated = DateTime.Now;

                    if (product.CategoryIDs == null) product.CategoryIDs = new int[] { };

                    var oldCateIds = productUpdate.ProductCategoryProducts.Select(c => c.CategoryID).ToArray();
                    var newCateIds = product.CategoryIDs;

                    var removeProductCategoryProducts = from productCate in productUpdate.ProductCategoryProducts
                                                        where (!newCateIds.Contains(productCate.CategoryID))
                                                        select productCate;
                    _context.ProductCategoryProducts.RemoveRange(removeProductCategoryProducts);
                    var addCateIds = from CateId in newCateIds
                                     where !oldCateIds.Contains(CateId)
                                     select CateId;

                    foreach (var CateId in addCateIds)
                    {
                        _context.ProductCategoryProducts.Add(new ProductCategoryProduct()
                        {
                            ProductID = id,
                            CategoryID = CateId
                        });
                    }

                    _context.Update(productUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                StatusMessage = "Vừa cập nhật sản phẩm";
                return RedirectToAction(nameof(Index));
            }
            var categories = await _context.CategoryProducts.ToListAsync();
            ViewData["categories"] = new MultiSelectList(categories, "Id", "Title");
            return View(product);
        }

        // GET: Post/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Author)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Post/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            StatusMessage = "Bạn vừa xóa sản phẩm: " + product.Title;
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }

        public class UploadOneFile
        {
            [Required(ErrorMessage = "Phải chọn file upload")]
            [DataType(DataType.Upload)]
            [Display(Name = "Chọn file upload")]
            public IFormFile FileUpload { get; set; }
        }
        [HttpGet]
        public IActionResult UploadPhoto(int id)
        {
            var product = _context.Products.Where(p => p.ProductId == id)
                                        .Include(p => p.Photos)
                                        .FirstOrDefault();
            if (product == null)
            {
                return NotFound("Không có sản phẩm");
            }
            ViewData["product"] = product;

            return View(new UploadOneFile());
        }
        [HttpPost, ActionName("UploadPhoto")]
        public async Task<IActionResult> UploadPhotoAsyn(int id, [Bind("FileUpload")] UploadOneFile model)
        {
            var product = _context.Products.Where(p => p.ProductId == id)
                                        .Include(p => p.Photos)
                                        .FirstOrDefault();
            if (product == null)
            {
                return NotFound("Không có sản phẩm");
            }
            ViewData["product"] = product;
            if (model != null)
            {
                var file1 = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.GetExtension(model.FileUpload.FileName);
                var file = Path.Combine("Uploads", "Products", file1);
                using (var filestream = new FileStream(file, FileMode.Create))
                {
                    await model.FileUpload.CopyToAsync(filestream);
                }

                _context.Add(new ProductPhoto()
                {
                    ProductID = product.ProductId,
                    FileName = file1
                });
                await _context.SaveChangesAsync();

            }
            return View(new UploadOneFile());
        }
        [HttpPost]
        public IActionResult ListPhotos(int id)
        {
            var product = _context.Products.Where(p => p.ProductId == id)
                            .Include(p => p.Photos)
                            .FirstOrDefault();
            if (product == null)
            {
                return Json(new
                {
                    success = 0,
                    message = "Product not found"
                });
            }
            var listPhoto = product.Photos.Select(p => new
            {
                id = p.Id,
                path = "/contents/Products/" + p.FileName
            });
            return Json(new
            {
                success = 1,
                photos = listPhoto
            });
        }
        [HttpPost]
        public IActionResult DeletePhoto(int id)
        {
            var photo = _context.ProductPhotos.Where(p => p.Id == id).FirstOrDefault();
            if (photo != null)
            {
                _context.Remove(photo);
                _context.SaveChanges();
                var fileName = "Uploads/Products/" + photo.FileName;
                System.IO.File.Delete(fileName);
            }
            return Ok();
        }
        [HttpPost]
        public async Task<IActionResult> UploadPhotoApi(int id, [Bind("FileUpload")] UploadOneFile model)
        {
            var product = _context.Products.Where(p => p.ProductId == id)
                                        .Include(p => p.Photos)
                                        .FirstOrDefault();
            if (product == null)
            {
                return NotFound("Không có sản phẩm");
            }

            if (model != null)
            {
                var file1 = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.GetExtension(model.FileUpload.FileName);
                var file = Path.Combine("Uploads", "Products", file1);
                using (var filestream = new FileStream(file, FileMode.Create))
                {
                    await model.FileUpload.CopyToAsync(filestream);
                }

                _context.Add(new ProductPhoto()
                {
                    ProductID = product.ProductId,
                    FileName = file1
                });
                await _context.SaveChangesAsync();

            
            }
            return Ok();
        }
        public class UploadOneFile1
        {
            [Required(ErrorMessage = "Phải chọn file upload")]
            [DataType(DataType.Upload)]
            [Display(Name = "Chọn file upload")]
            public IFormFile[] FileUploads { get; set; }
        }

        // [HttpPost]
        // public async Task<IActionResult> UploadPhotoApi(int id, [Bind("FileUploads")] UploadOneFile1 model)
        // {
        //     var product = _context.Products.Where(p => p.ProductId == id)
        //                                 .Include(p => p.Photos) 
        //                                 .FirstOrDefault();
        //     if (product == null)
        //     {
        //         return NotFound("Không có sản phẩm");
        //     }

        //     // if (model != null)
        //     // {
        //     //     var file1 = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.GetExtension(model.FileUploads.FileName);
        //     //     var file = Path.Combine("Uploads", "Products", file1);
        //     //     using (var filestream = new FileStream(file, FileMode.Create))
        //     //     {
        //     //         await model.FileUploads.CopyToAsync(filestream);
        //     //     }

        //     //     _context.Add(new ProductPhoto()
        //     //     {
        //     //         ProductID = product.ProductId,
        //     //         FileName = file1
        //     //     });
        //     //     await _context.SaveChangesAsync();

            
        //     // }
        //     return Ok();
        // }
    }
}
