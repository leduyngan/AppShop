using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Data;
using App.Models;
using App.Models.Blog;
using App.Models.Product;
using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Areas.Database.Controllers
{
    [Area("Database")]
    [Route("/database-manage/[action]")]
    public class DbManageController : Controller
    {
        private readonly AppDbContext _dbConetxt;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DbManageController(AppDbContext dbConetxt, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _dbConetxt = dbConetxt;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult DeleteDb()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> DeleteDbAsnyc()
        {
            var success = await _dbConetxt.Database.EnsureDeletedAsync();
            StatusMessage = success ? "Xóa database thành công" : "Không xóa được Db";

            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> Migrate()
        {
            await _dbConetxt.Database.MigrateAsync();
            StatusMessage = "Cập nhật database thành công";

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> SeedDataAsync()
        {
            var roleNames = typeof(RoleName).GetFields().ToList();
            foreach (var r in roleNames)
            {
                var roleName = (string)r.GetRawConstantValue();
                var rFound = await _roleManager.FindByNameAsync(roleName);
                if (rFound == null)
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
            var userAdmin = await _userManager.FindByNameAsync("admin");
            if (userAdmin == null)
            {
                userAdmin = new AppUser()
                {
                    UserName = "admin",
                    Email = "admin@example.com",
                    EmailConfirmed = true,
                };
                await _userManager.CreateAsync(userAdmin, "admin123");
                await _userManager.AddToRoleAsync(userAdmin, RoleName.Administrator);

            }
            SeedPostCategory();
            SeedProductCategory();
            StatusMessage = "Vừa seed database";
            return RedirectToAction("Index");
        }

        private void SeedPostCategory()
        {
            _dbConetxt.Categories.RemoveRange(_dbConetxt.Categories.Where(c => c.Description.Contains("[fakeData]")));
            _dbConetxt.Posts.RemoveRange(_dbConetxt.Posts.Where(c => c.Content.Contains("[fakeData]")));
            _dbConetxt.SaveChanges();
            var fakerCategory = new Faker<Category>();
            int cm = 1;
            fakerCategory.RuleFor(c => c.Title, fk => $"CM{cm++} " + fk.Lorem.Sentence(1, 2).Trim('.'));
            fakerCategory.RuleFor(c => c.Description, fk => fk.Lorem.Sentences(5) + "[fakeData]");
            fakerCategory.RuleFor(c => c.Slug, fk => fk.Lorem.Slug());

            var cate1 = fakerCategory.Generate();
            var cate11 = fakerCategory.Generate();
            var cate12 = fakerCategory.Generate();
            var cate2 = fakerCategory.Generate();
            var cate21 = fakerCategory.Generate();
            var cate22 = fakerCategory.Generate();

            cate11.ParentCategory = cate1;
            cate12.ParentCategory = cate1;
            cate21.ParentCategory = cate2;
            cate22.ParentCategory = cate2;
            var categories = new Category[] { cate1, cate11, cate12, cate2, cate21, cate22 };
            _dbConetxt.Categories.AddRange(categories);


            // POST
            var rCateIndex = new Random();
            int bv = 1;

            var user = _userManager.GetUserAsync(this.User).Result;
            var fakerPost = new Faker<Post>();
            fakerPost.RuleFor(p => p.AuthorId, f => user.Id);
            fakerPost.RuleFor(p => p.Content, f => f.Lorem.Paragraphs(7) + "[fakeData]");
            fakerPost.RuleFor(p => p.DateCreated, f => f.Date.Between(new DateTime(2022, 1, 1), DateTime.Now));
            fakerPost.RuleFor(p => p.Description, f => f.Lorem.Sentences(3)); ;
            fakerPost.RuleFor(p => p.Published, f => true);
            fakerPost.RuleFor(p => p.Slug, f => f.Lorem.Slug());
            fakerPost.RuleFor(p => p.Title, f => $"Bài {bv++} " + f.Lorem.Sentence(3, 4).Trim('.'));

            List<Post> posts = new List<Post>();
            List<PostCategory> postCategories = new List<PostCategory>();

            for (int i = 0; i < 40; i++)
            {
                var post = fakerPost.Generate();
                post.DateUpdated = post.DateCreated;
                posts.Add(post);
                postCategories.Add(new PostCategory()
                {
                    Post = post,
                    Category = categories[rCateIndex.Next(5)]
                });
            }
            _dbConetxt.AddRange(posts);
            _dbConetxt.AddRange(postCategories);

            _dbConetxt.SaveChanges();
        }
        private void SeedProductCategory()
        {
            _dbConetxt.CategoryProducts.RemoveRange(_dbConetxt.CategoryProducts.Where(c => c.Description.Contains("[fakeData]")));
            _dbConetxt.Products.RemoveRange(_dbConetxt.Products.Where(c => c.Content.Contains("[fakeData]")));
            _dbConetxt.SaveChanges();
            var fakerCategory = new Faker<CategoryProduct>();
            int cm = 1;
            fakerCategory.RuleFor(c => c.Title, fk => $"Nhom SP{cm++} " + fk.Lorem.Sentence(1, 2).Trim('.'));
            fakerCategory.RuleFor(c => c.Description, fk => fk.Lorem.Sentences(5) + "[fakeData]");
            fakerCategory.RuleFor(c => c.Slug, fk => fk.Lorem.Slug());

            var cate1 = fakerCategory.Generate();
            var cate11 = fakerCategory.Generate();
            var cate12 = fakerCategory.Generate();
            var cate2 = fakerCategory.Generate();
            var cate21 = fakerCategory.Generate();
            var cate22 = fakerCategory.Generate();

            cate11.ParentCategory = cate1;
            cate12.ParentCategory = cate1;
            cate21.ParentCategory = cate2;
            cate22.ParentCategory = cate2;
            var categories = new CategoryProduct[] { cate1, cate11, cate12, cate2, cate21, cate22 };
            _dbConetxt.CategoryProducts.AddRange(categories);


            // POST
            var rCateIndex = new Random();
            int bv = 1;

            var user = _userManager.GetUserAsync(this.User).Result;
            var fakerProduct = new Faker<ProductModel>();
            fakerProduct.RuleFor(p => p.AuthorId, f => user.Id);
            fakerProduct.RuleFor(p => p.Content, f => f.Commerce.ProductDescription() + "[fakeData]");
            fakerProduct.RuleFor(p => p.DateCreated, f => f.Date.Between(new DateTime(2022, 1, 1), DateTime.Now));
            fakerProduct.RuleFor(p => p.Description, f => f.Lorem.Sentences(3)); ;
            fakerProduct.RuleFor(p => p.Published, f => true);
            fakerProduct.RuleFor(p => p.Slug, f => f.Lorem.Slug());
            fakerProduct.RuleFor(p => p.Title, f => $"SP {bv++} " + f.Commerce.ProductName());
            fakerProduct.RuleFor(p => p.Price, f => int.Parse(f.Commerce.Price(500, 1000, 0)));

            List<ProductModel> products = new List<ProductModel>();
            List<ProductCategoryProduct> productCategoryProducts = new List<ProductCategoryProduct>();

            for (int i = 0; i < 40; i++)
            {
                var product = fakerProduct.Generate();
                product.DateUpdated = product.DateCreated;
                products.Add(product);
                productCategoryProducts.Add(new ProductCategoryProduct()
                {
                    Product = product,
                    Category = categories[rCateIndex.Next(5)]
                });
            }
            _dbConetxt.AddRange(products);
            _dbConetxt.AddRange(productCategoryProducts);

            _dbConetxt.SaveChanges();
        }

    }
}