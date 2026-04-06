using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020105.BusinessLayers;
using SV22T1020105.Models.Sales;

namespace SV22T1020105.Shop.Controllers
{
    public class OrderController : Controller
    {
        #region Checkout

        [Authorize]
        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = ShoppingCartHelper.GetShoppingCart();
            if (!cart.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống, vui lòng thêm sản phẩm trước khi đặt hàng";
                return RedirectToAction("ViewCart", "Cart");
            }
            ViewBag.Cart = cart;
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Checkout(string deliveryProvince, string deliveryAddress)
        {
            var cart = ShoppingCartHelper.GetShoppingCart();
            if (!cart.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống";
                return RedirectToAction("ViewCart", "Cart");
            }

            ViewBag.Cart = cart;

            if (string.IsNullOrWhiteSpace(deliveryProvince))
                ModelState.AddModelError("deliveryProvince", "Vui lòng nhập tỉnh/thành giao hàng");
            if (string.IsNullOrWhiteSpace(deliveryAddress))
                ModelState.AddModelError("deliveryAddress", "Vui lòng nhập địa chỉ giao hàng");

            if (!ModelState.IsValid)
                return View();

            try
            {
                var userData = User.GetCustomerData();
                int customerID = int.Parse(userData!.UserId);

                // Tạo đơn hàng mới
                int orderID = await SalesDataService.AddOrderAsync(customerID, deliveryProvince, deliveryAddress);

                // Thêm chi tiết đơn hàng
                foreach (var item in cart)
                {
                    await SalesDataService.AddDetailAsync(new OrderDetail
                    {
                        OrderID = orderID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        SalePrice = item.SalePrice
                    });
                }

                // Xóa giỏ hàng
                ShoppingCartHelper.ClearCart();

                return RedirectToAction("OrderSuccess", new { id = orderID });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Đặt hàng thất bại: {ex.Message}");
                return View();
            }
        }

        #endregion

        #region Order Success

        public async Task<IActionResult> OrderSuccess(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return RedirectToAction("History");
            ViewBag.Details = await SalesDataService.ListDetailsAsync(id);
            return View(order);
        }

        #endregion

        #region Order History

        [Authorize]
        public async Task<IActionResult> History()
        {
            var userData = User.GetCustomerData();
            int customerID = int.Parse(userData!.UserId);
            var orders = await SalesDataService.ListOrdersByCustomerAsync(customerID);
            return View(orders);
        }

        #endregion

        #region Track Order

        public async Task<IActionResult> TrackOrder(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                return RedirectToAction("History");
            }
            ViewBag.Details = await SalesDataService.ListDetailsAsync(id);
            return View(order);
        }

        #endregion
    }
}
