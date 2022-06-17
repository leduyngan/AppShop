using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using App.Models;
using Microsoft.Extensions.Logging;
using App.Models.Blog;
using Microsoft.EntityFrameworkCore;
using App.Helper;

namespace App.Areas.Blog.Controllers
{
    [Area("Blog")]
    public class ViewPostController : Controller
    {
        private readonly ILogger<ViewPostController> _logger;
        private readonly AppDbContext _context;

        public ViewPostController(ILogger<ViewPostController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [Route("/post/{categoryslug?}")]
        public IActionResult Index(string categoryslug, [FromQuery(Name = "p")] int currentPage, int pageSize)
        {
            var categories = GetCategories();
            ViewBag.categories = categories;
            ViewBag.categoryslug = categoryslug;
            Category category = null;
            if (!string.IsNullOrEmpty(categoryslug))
            {
                category = _context.Categories.Where(c => c.Slug == categoryslug)
                                                .Include(c => c.CategoryChildren)
                                                .FirstOrDefault();

                if (category == null)
                {
                    return NotFound("Không thấy category");
                }
            }
            var posts = _context.Posts
                                .Include(p => p.Author)
                                .Include(p => p.PostCategories)
                                .ThenInclude(p => p.Category)
                                .AsQueryable();
            //posts.OrderByDescending(p => p.DateUpdated);

            if (category != null)
            {
                var ids = new List<int>();
                ids = GetCategoryChild(category);
                //category.ChildCategoryIDs(null, ids); // có thể dùng cách này thay cho ids = GetCategoryChild(category);
                ids.Add(category.Id);
                posts = posts.Where(p => p.PostCategories.Where(pc => ids.Contains(pc.CategoryID)).Any());

            }

            int totalPosts = posts.Count();
            if (pageSize <= 0) pageSize = 10;
            int countPages = (int)Math.Ceiling((double)totalPosts / pageSize);

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
            ViewBag.totalPosts = totalPosts;
            ViewBag.postIndex = (currentPage - 1) * pageSize;
            var postsInPage = posts.OrderByDescending(p => p.DateUpdated).Skip((currentPage - 1) * pageSize)
                                    .Take(pageSize);

            ViewBag.category = category;



            return View(postsInPage.ToList());
        }

        public List<int> GetCategoryChild(Category category)
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
        [Route("/post/{postslug}.html")]
        public IActionResult Detail(string postslug)
        {
            var categories = GetCategories();
            ViewBag.categories = categories;
            var post = _context.Posts.Where(p => p.Slug == postslug)
                                    .Include(p => p.Author)
                                    .Include(p => p.PostCategories)
                                    .ThenInclude(p => p.Category)
                                    .FirstOrDefault();
            if (post == null)
            {
                return NotFound("Không tìm thấy bài viết");
            }
            Category category = post.PostCategories.FirstOrDefault().Category;
            ViewBag.category = category;
            var otherPosts = _context.Posts.Where(p => p.PostCategories.Any(c => c.CategoryID == category.Id))
                                            .Where(p => p.PostId != post.PostId)
                                            .OrderByDescending(p => p.DateUpdated)
                                            .Take(5);
            ViewBag.otherPosts = otherPosts;



            return View(post);
        }
        private List<Category> GetCategories()
        {
            var categories = _context.Categories
                            .Include(c => c.CategoryChildren)
                            .AsEnumerable()
                            .Where(c => c.ParentCategoryId == null)
                            .ToList();
            return categories;

        }
    }
}