using System.Collections.Generic;
using App.Models.Product;
using Microsoft.AspNetCore.Mvc;

namespace App.Components
{
    public class CategoryProductSidebar : ViewComponent
    {
        public class CategoryProductSidebarData
        {
            public List<CategoryProduct> Categories { set; get; }
            public int Lever { get; set; }
            public string CategorySlug { get; set; }
        }
        public IViewComponentResult Invoke(CategoryProductSidebarData data)
        {
            return View(data);
        }
    }
}