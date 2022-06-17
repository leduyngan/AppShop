using System.Collections.Generic;
using App.Models.Blog;
using Microsoft.AspNetCore.Mvc;

namespace App.Components
{
    public class CategorySidebar : ViewComponent
    {
        public class CategorySidebarData
        {
            public List<Category> Categories { set; get; }
            public int Lever { get; set; }
            public string CategorySlug { get; set; }
        }
        public IViewComponentResult Invoke(CategorySidebarData data)
        {
            return View(data);
        }
    }
}