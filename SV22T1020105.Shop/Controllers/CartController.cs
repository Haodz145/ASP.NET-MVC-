using Microsoft.AspNetCore.Mvc;
using SV22T1020105.BusinessLayers;
using SV22T1020105.Models.Sales;

namespace SV22T1020105.Shop.Controllers
{
    public class CartController : Controller
    {
        public IActionResult ViewCart()
        {
            var cart = ShoppingCartHelper.GetShoppingCart();
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await CatalogDataService.GetProductAsync(productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại";
                return RedirectToAction("Index", "Product");
            }

            ShoppingCartHelper.AddItemToCart(new OrderDetailViewInfo
            {
                ProductID = productId,
                ProductName = product.ProductName,
                Unit = product.Unit,
                Photo = product.Photo ?? "nophoto.png",
                Quantity = quantity,
                SalePrice = product.Price
            });

            TempData["SuccessMessage"] = $"Đã thêm \"{product.ProductName}\" vào giỏ hàng";
            return RedirectToAction("ViewCart");
        }

        [HttpPost]
        public IActionResult UpdateCart(int productId, int quantity, decimal salePrice)
        {
            if (quantity <= 0)
                ShoppingCartHelper.RemoveItemFromCart(productId);
            else
                ShoppingCartHelper.UpdateCartItem(productId, quantity, salePrice);
            return RedirectToAction("ViewCart");
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            ShoppingCartHelper.RemoveItemFromCart(productId);
            return RedirectToAction("ViewCart");
        }

        [HttpPost]
        public IActionResult ClearCart()
        {
            ShoppingCartHelper.ClearCart();
            return RedirectToAction("ViewCart");
        }
    }
}
