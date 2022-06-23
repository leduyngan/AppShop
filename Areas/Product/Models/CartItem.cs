using App.Models.Product;

namespace App.Areas.Product.Models
{
    public class CartItem
    {
        public int quantity { set; get; }
        public string photo { set; get; }
        public ProductModel product { set; get; }
        public bool IsChecked { set; get; }
    }
}
