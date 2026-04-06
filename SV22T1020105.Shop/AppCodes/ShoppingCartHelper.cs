using SV22T1020105.Models.Sales;

namespace SV22T1020105.Shop
{
    /// <summary>
    /// Tiện ích quản lý giỏ hàng lưu trong Session
    /// </summary>
    public static class ShoppingCartHelper
    {
        private const string CART = "ShoppingCart";

        public static List<OrderDetailViewInfo> GetShoppingCart()
        {
            var cart = ApplicationContext.GetSessionData<List<OrderDetailViewInfo>>(CART);
            if (cart == null)
            {
                cart = new List<OrderDetailViewInfo>();
                ApplicationContext.SetSessionData(CART, cart);
            }
            return cart;
        }

        public static int CartCount => GetShoppingCart().Sum(x => x.Quantity);

        public static void AddItemToCart(OrderDetailViewInfo data)
        {
            var cart = GetShoppingCart();
            var existing = cart.Find(m => m.ProductID == data.ProductID);
            if (existing == null)
                cart.Add(data);
            else
            {
                existing.Quantity += data.Quantity;
                existing.SalePrice = data.SalePrice;
            }
            ApplicationContext.SetSessionData(CART, cart);
        }

        public static void UpdateCartItem(int productID, int quantity, decimal salePrice)
        {
            var cart = GetShoppingCart();
            var item = cart.Find(m => m.ProductID == productID);
            if (item != null)
            {
                item.Quantity = quantity;
                item.SalePrice = salePrice;
                ApplicationContext.SetSessionData(CART, cart);
            }
        }

        public static void RemoveItemFromCart(int productID)
        {
            var cart = GetShoppingCart();
            int idx = cart.FindIndex(m => m.ProductID == productID);
            if (idx >= 0)
            {
                cart.RemoveAt(idx);
                ApplicationContext.SetSessionData(CART, cart);
            }
        }

        public static void ClearCart()
        {
            ApplicationContext.SetSessionData(CART, new List<OrderDetailViewInfo>());
        }
    }
}
