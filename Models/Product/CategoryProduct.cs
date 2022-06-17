using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Models.Product
{
    [Table("CategoryProduct")]
    public class CategoryProduct
    {
        [Key]
        public int Id { set; get; }

        [Required(ErrorMessage = "Phải có tên danh mục")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "{0} dài {1} đến {2}")]
        [Display(Name = "Tên danh mục")]
        public string Title { get; set; }
        [DataType(DataType.Text)]
        [Display(Name = "Nội dung danh mục")]
        public string Description { get; set; }

        // chuỗi Url
        [Required(ErrorMessage = "Phải tạo url")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "{0} phải dài từ {1} đền {2}")]
        [RegularExpression(@"^[a-z0-9-]*$", ErrorMessage = "Chỉ dùng các ký tự [a-z0-9-]")]
        [Display(Name = "Url hiển thị")]
        public string Slug { get; set; }
        // Các Category con
        public ICollection<CategoryProduct> CategoryChildren { get; set; }

        [Display(Name = "Danh mục cha")]
        public int? ParentCategoryId { get; set; }

        [ForeignKey("ParentCategoryId")]
        [Display(Name = "Danh mục cha")]
        public CategoryProduct ParentCategory { get; set; }

        public void ChildCategoryIDs(ICollection<CategoryProduct> chilCates, List<int> lists)
        {
            if (chilCates == null)
            {
                chilCates = this.CategoryChildren;
            }
            foreach (CategoryProduct category in chilCates)
            {
                lists.Add(category.Id);
                ChildCategoryIDs(category.CategoryChildren, lists);
            }
        }
        public List<CategoryProduct> ListParent()
        {
            List<CategoryProduct> li = new List<CategoryProduct>();
            var parent = this.ParentCategory;
            while (parent != null)
            {
                li.Add(parent);
                parent = parent.ParentCategory;
            }
            li.Reverse();
            return li;
        }
    }
}