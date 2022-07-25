using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using App.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using App.Helper;
using App.Models.Product;
using App.Areas.Product.Models;
using App.Areas.Srevice;
using Microsoft.AspNetCore.Authorization;

namespace App.Areas.Blog.Controllers
{

    [Area("Product")]
    public class ViewProductController : Controller
    {
        private readonly ILogger<ViewProductController> _logger;
        private readonly AppDbContext _context;
        private CartService _cartService;

        public ViewProductController(ILogger<ViewProductController> logger, AppDbContext context, CartService cartService)
        {
            _logger = logger;
            _context = context;
            _cartService = cartService;
        }

        // [Route("/LayoutProduct")]
        // public IActionResult sp1()
        // {
        //     return View("LayoutProduct");
        // }


        [Route("/product/{categoryslug?}")]
        public IActionResult Index(string categoryslug, [FromQuery(Name = "p")] int currentPage, int pageSize)
        {
            var categories = GetCategories();
            ViewBag.categories = categories;
            ViewBag.categoryslug = categoryslug;
            CategoryProduct category = null;
            if (!string.IsNullOrEmpty(categoryslug))
            {
                category = _context.CategoryProducts.Where(c => c.Slug == categoryslug)
                                                .Include(c => c.CategoryChildren)
                                                .FirstOrDefault();

                if (category == null)
                {
                    return NotFound("Không thấy category");
                }
            }
            var products = _context.Products
                                .Include(p => p.Author)
                                .Include(p => p.Photos)
                                .Include(p => p.ProductCategoryProducts)
                                .ThenInclude(p => p.Category)
                                .AsQueryable();


            if (category != null)
            {
                var ids = new List<int>();
                ids = GetCategoryChild(category);
                //category.ChildCategoryIDs(null, ids); // có thể dùng cách này thay cho ids = GetCategoryChild(category);
                ids.Add(category.Id);
                products = products.Where(p => p.ProductCategoryProducts.Where(pc => ids.Contains(pc.CategoryID)).Any());

            }

            int totalProducts = products.Count();
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
            var productsInPage = products.OrderByDescending(p => p.DateUpdated).Skip((currentPage - 1) * pageSize)
                                    .Take(pageSize);

            ViewBag.category = category;



            return View(productsInPage.ToList());
        }

        public List<int> GetCategoryChild(CategoryProduct category)
        {
            var cateChilIDs = new List<int>();
            if (category.CategoryChildren != null)
            {
                var cateChils = category.CategoryChildren.ToList();
                foreach (var cate in cateChils)
                {
                    cateChilIDs.Add(cate.Id);
                    GetCategoryChild(cate);
                }
            }
            return cateChilIDs;
        }

        [Route("/product/{productslug}.html")]
        public IActionResult Detail(string productslug)
        {
            var categories = GetCategories();
            ViewBag.categories = categories;
            var product = _context.Products.Where(p => p.Slug == productslug)
                                    .Include(p => p.Author)
                                    .Include(p => p.Photos)
                                    .Include(p => p.ProductCategoryProducts)
                                    .ThenInclude(p => p.Category)
                                    .FirstOrDefault();
            if (product == null)
            {
                return NotFound("Không tìm thấy bài viết");
            }
            CategoryProduct category = product.ProductCategoryProducts.FirstOrDefault().Category;
            ViewBag.category = category;
            var otherProducts = _context.Products.Where(p => p.ProductCategoryProducts.Any(c => c.CategoryID == category.Id))
                                            .Where(p => p.ProductId != product.ProductId)
                                            .OrderByDescending(p => p.DateUpdated)
                                            .Take(5);
            ViewBag.otherProducts = otherProducts;



            return View(product);
        }
        private List<CategoryProduct> GetCategories()
        {
            var categories = _context.CategoryProducts
                            .Include(c => c.CategoryChildren)
                            .AsEnumerable()
                            .Where(c => c.ParentCategoryId == null)
                            .ToList();
            return categories;

        }


        // Thêm sản phẩm vào cart
        [Authorize]
        [Route("addcart/{productid:int}", Name = "addcart")]
        public IActionResult AddToCart([FromRoute] int productid)
        {
            var productWithPhoto = _context.Products
                .Where(p => p.ProductId == productid)
                .Include(p => p.Photos)
                .FirstOrDefault();
            var product = new ProductModel
            {
                ProductId = productWithPhoto.ProductId,
                Title = productWithPhoto.Title,
                Description = productWithPhoto.Description,
                Slug = productWithPhoto.Slug,
                Content = productWithPhoto.Content,
                Price = productWithPhoto.Price
            };

            var productPhoto = productWithPhoto.Photos.FirstOrDefault();
            if (product == null)
                return NotFound("Không có sản phẩm");

            // Xử lý đưa vào Cart ...
            var cart = _cartService.GetCartItems();
            var cartitem = cart.Find(p => p.product.ProductId == productid);
            if (cartitem != null)
            {
                // Đã tồn tại, tăng thêm 1
                cartitem.quantity++;
            }
            else
            {
                //  Thêm mới
                cart.Add(new CartItem() { quantity = 1, product = product, photo = productPhoto.FileName.ToString() });
            }
            // Lưu cart vào Session
            _cartService.SaveCartSession(cart);
            // Chuyển đến trang hiện thị Cart
            return RedirectToAction(nameof(Cart));
        }
        // Hiện thị giỏ hàng
        [Authorize]
        [Route("/cart", Name = "cart")]
        public IActionResult Cart()
        {
            var cart = _cartService.GetCartItems();
            return View(cart);
        }
        [Route("/updatestatuscart", Name = "updatestatuscart")]
        [HttpPost]
        public IActionResult Updatestatuscart([FromForm] bool isChecked)
        {
            var carts = _cartService.GetCartItems();
            foreach (var cart in carts)
            {
                cart.IsChecked = isChecked;
                _cartService.SaveCartSession(carts);
            }
            var carsdft = _cartService.GetCartItems();
            return Ok();
        }
        /// Cập nhật

        [Route("/updatecart", Name = "updatecart")]
        [HttpPost]
        public IActionResult UpdateCart([FromForm] int productid, [FromForm] int quantity, [FromForm] bool isChecked)
        {
            if (!User.Identity.IsAuthenticated)
            {
                Redirect("https://localhost:5001/Account/login");
            }


            var productWithPhoto = _context.Products
                .Where(p => p.ProductId == productid)
                .Include(p => p.Photos)
                .FirstOrDefault();
            var product = new ProductModel
            {
                ProductId = productWithPhoto.ProductId,
                Title = productWithPhoto.Title,
                Description = productWithPhoto.Description,
                Slug = productWithPhoto.Slug,
                Content = productWithPhoto.Content,
                Price = productWithPhoto.Price
            };

            var productPhoto = productWithPhoto.Photos.FirstOrDefault();

            // Cập nhật Cart thay đổi số lượng quantity ...
            var cart = _cartService.GetCartItems();
            var cartNumber = cart.Count();
            var cartitem = cart.Find(p => p.product.ProductId == productid);
            if (cartitem != null)
            {
                cartitem.quantity = quantity;
                cartitem.IsChecked = isChecked;
            }
            else
            {
                //  Thêm mới
                cart.Add(new CartItem() { quantity = quantity, product = product, photo = productPhoto.FileName.ToString(), IsChecked = isChecked });
                cartNumber++;
            }
            _cartService.SaveCartSession(cart);
            var carsdft = _cartService.GetCartItems();
            // Trả về mã thành công (không có nội dung gì - chỉ để Ajax gọi)
            return Content(cartNumber.ToString());
        }
        /// xóa item trong cart
        [Route("/removecart/{productid:int}", Name = "removecart")]
        public IActionResult RemoveCart([FromRoute] int productid)
        {
            var cart = _cartService.GetCartItems();
            var cartitem = cart.Find(p => p.product.ProductId == productid);
            if (cartitem != null)
            {
                // Đã tồn tại, tăng thêm 1
                cart.Remove(cartitem);
            }

            _cartService.SaveCartSession(cart);
            return RedirectToAction(nameof(Cart));
        }

        [Route("/Checkout")]
        public IActionResult Checkout()
        {
            var cart = _cartService.GetCartItems();
            _cartService.ClearCart();
            return Content("Đã gửi đơn hàng");
        }
        [Route("/ReloadCard")]
        public ActionResult ReloadCard()
        {
            return PartialView("_CartShopPartial");
        }
        [Route("/GetCartItem")]
        public List<CartItem> GetCartItem()
        {
            var cart = _cartService.GetCartItems();
            return cart;
        }
    }
}