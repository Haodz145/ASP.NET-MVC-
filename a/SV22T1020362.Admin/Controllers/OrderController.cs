using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020362.BusinessLayers;
using SV22T1020362.Models.Catalog;
using SV22T1020362.Models.Common;
using SV22T1020362.Models.Partner;
using SV22T1020362.Models.Sales;

namespace SV22T1020362.Admin.Controllers
{
    /// <summary>
    /// Các chức năng liên quan đến quản lý đơn hàng.
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Sales},{WebUserRoles.Administrator}")]
    public class OrderController : Controller
    {
        public const string SEARCH_ORDER = "SearchOrder";
        private const string SEARCH_PRODUCT = "SearchProductToSale";

        #region Tìm kiếm đơn hàng

        /// <summary>
        /// Giao diện nhập đầu vào tìm kiếm đơn hàng
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<OrderSearchInput>(SEARCH_ORDER);
            if (input == null)
                input = new OrderSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = "",
                    Status = 0,
                    DateFrom = null,
                    DateTo = null
                };
            return View(input);
        }

        /// <summary>
        /// Tìm kiếm và hiển thị danh sách đơn hàng dưới dạng phân trang
        /// </summary>
        public async Task<IActionResult> Search(OrderSearchInput input)
        {
            if (input.DateFrom.HasValue)
                input.DateFrom = DateTime.SpecifyKind(input.DateFrom.Value, DateTimeKind.Unspecified);
            if (input.DateTo.HasValue)
                input.DateTo = DateTime.SpecifyKind(input.DateTo.Value, DateTimeKind.Unspecified);

            var result = await SalesDataService.ListOrdersAsync(input);
            ApplicationContext.SetSessionData(SEARCH_ORDER, input);
            return View(result);
        }

        #endregion

        #region Lập đơn hàng mới

        /// <summary>
        /// Giao diện tích hợp các chức năng để tạo đơn hàng
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(SEARCH_PRODUCT);
            if (input == null)
            {
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = 3,
                    CategoryID = 0,
                    MaxPrice = 0,
                    MinPrice = 0,
                    SearchValue = ""
                };
            }
            return View(input);
        }

        /// <summary>
        /// Tìm mặt hàng cần bán
        /// </summary>
        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(SEARCH_PRODUCT, input);
            return View(result);
        }

        /// <summary>
        /// Hiển thị giỏ hàng
        /// </summary>
        public IActionResult ShowCart()
        {
            var cart = ShoppingCartHelper.GetShoppingCart();
            return View(cart);
        }

        /// <summary>
        /// Thêm một mặt hàng vào giỏ hàng
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddCartItem(int productID, int quantity, decimal salePrice)
        {
            var product = await CatalogDataService.GetProductAsync(productID);
            if (product == null)
                return Json(new ApiResult(0, "Mặt hàng không tồn tại"));

            var item = new OrderDetailViewInfo()
            {
                ProductID = productID,
                ProductName = product.ProductName,
                Unit = product.Unit,
                Photo = product.Photo ?? "nophoto.png",
                Quantity = quantity,
                SalePrice = salePrice
            };
            ShoppingCartHelper.AddItemToCart(item);
            return Json(new ApiResult(1, ""));
        }

        /// <summary>
        /// Partial view form cập nhật chi tiết giỏ hàng
        /// </summary>
        [HttpGet]
        public IActionResult EditCartItem(int productId = 0)
        {
            var item = ShoppingCartHelper.GetCartItem(productId);
            return PartialView(item);
        }

        /// <summary>
        /// Cập nhật mặt hàng trong giỏ
        /// </summary>
        public IActionResult UpdateCartItem(int productID, int quantity, decimal salePrice)
        {
            ShoppingCartHelper.UpdateCartItem(productID, quantity, salePrice);
            return Json(new ApiResult(1, ""));
        }

        /// <summary>
        /// Xóa 1 mặt hàng khỏi giỏ hàng
        /// </summary>
        [AcceptVerbs("GET", "POST")]
        public IActionResult DeleteCartItem(int productId = 0)
        {
            if (Request.Method == "POST")
            {
                ShoppingCartHelper.RemoveItemFromCart(productId);
                return Json(new ApiResult(1, ""));
            }

            ViewBag.ProductID = productId;
            return PartialView();
        }

        /// <summary>
        /// Xóa toàn bộ giỏ hàng
        /// </summary>
        [AcceptVerbs("GET", "POST")]
        public IActionResult ClearCart()
        {
            if (Request.Method == "POST")
            {
                ShoppingCartHelper.ClearCart();
                return Json(new ApiResult(1, ""));
            }
            return PartialView();
        }

        /// <summary>
        /// Tạo đơn hàng mới từ giỏ hàng hiện tại.
        /// Sau khi tạo thành công, xóa giỏ hàng và trả về mã đơn hàng.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder(int customerID = 0, string province = "", string address = "")
        {
            var cart = ShoppingCartHelper.GetShoppingCart();
            if (cart.Count == 0)
                return Json(new ApiResult(0, "Giỏ hàng đang trống, không tạo được đơn hàng"));

            // Lấy EmployeeID từ Claims của nhân viên đang đăng nhập
            var userData = User.GetUserData();
            int employeeID = 0;
            if (userData != null && int.TryParse(userData.UserId, out int parsedId))
                employeeID = parsedId;

            // Tạo đơn hàng với thông tin cơ bản và nhân viên phụ trách
            int orderID = await SalesDataService.AddOrderAsync(customerID, province, address);

            // Thêm từng mặt hàng trong giỏ vào chi tiết đơn hàng
            foreach (var item in cart)
            {
                await SalesDataService.AddDetailAsync(new OrderDetail()
                {
                    OrderID = orderID,
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    SalePrice = item.SalePrice
                });
            }

            ShoppingCartHelper.ClearCart();
            return Json(new ApiResult(orderID, ""));
        }

        #endregion

        #region Customer API (dùng cho modal chọn/tạo khách hàng trong đơn hàng)

        /// <summary>
        /// Lấy 50 khách hàng đầu tiên để hiển thị trong dropdown
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTopCustomers()
        {
            var result = await PartnerDataService.ListCustomersAsync(new PaginationSearchInput
            {
                Page = 1,
                PageSize = 50,
                SearchValue = ""
            });
            // Trả về danh sách JSON gồm CustomerID và CustomerName
            var data = result.DataItems.Select(c => new
            {
                c.CustomerID,
                c.CustomerName,
                c.ContactName,
                c.Phone,
                c.Email,
                c.Province,
                c.Address
            });
            return Json(data);
        }

        /// <summary>
        /// Tìm kiếm tất cả khách hàng (dùng cho datalist/autocomplete trong modal)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchCustomers(string q = "")
        {
            var result = await PartnerDataService.ListCustomersAsync(new PaginationSearchInput
            {
                Page = 1,
                PageSize = 0, // Lấy tất cả
                SearchValue = q
            });
            var data = result.DataItems.Select(c => new
            {
                c.CustomerID,
                c.CustomerName,
                c.ContactName,
                c.Phone,
                c.Email,
                c.Province,
                c.Address,
                c.IsLocked
            });
            return Json(data);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một khách hàng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCustomer(int id)
        {
            var customer = await PartnerDataService.GetCustomerAsync(id);
            if (customer == null)
                return Json(null);
            return Json(new
            {
                customer.CustomerID,
                customer.CustomerName,
                customer.ContactName,
                customer.Phone,
                customer.Email,
                customer.Province,
                customer.Address,
                customer.IsLocked
            });
        }

        /// <summary>
        /// Tạo khách hàng mới từ modal (trả về CustomerID mới)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCustomerFromModal(
            string customerName, string contactName, string phone,
            string email, string province, string address)
        {
            // Validate dữ liệu
            if (string.IsNullOrWhiteSpace(customerName))
                return Json(new { code = 0, message = "Tên khách hàng không được để trống." });
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { code = 0, message = "Email không được để trống." });
            if (string.IsNullOrWhiteSpace(province))
                return Json(new { code = 0, message = "Vui lòng chọn Tỉnh/Thành." });

            // Kiểm tra email trùng
            bool emailValid = await PartnerDataService.ValidateCustomerEmailAsync(email);
            if (!emailValid)
                return Json(new { code = 0, message = "Email này đã được sử dụng bởi khách hàng khác." });

            try
            {
                var newCustomer = new Customer()
                {
                    CustomerName = customerName,
                    ContactName = contactName ?? "",
                    Phone = phone ?? "",
                    Email = email,
                    Province = province,
                    Address = address ?? "",
                    IsLocked = false
                };
                int newId = await PartnerDataService.AddCustomerAsync(newCustomer);

                // Lấy lại thông tin khách hàng vừa tạo
                var created = await PartnerDataService.GetCustomerAsync(newId);
                return Json(new
                {
                    code = 1,
                    message = "",
                    data = new
                    {
                        created!.CustomerID,
                        created.CustomerName,
                        created.ContactName,
                        created.Phone,
                        created.Email,
                        created.Province,
                        created.Address
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { code = 0, message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật CustomerID cho đơn hàng (AJAX)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateOrderCustomer(int orderID, int customerID)
        {
            try
            {
                bool result = await SalesDataService.UpdateOrderCustomerAsync(orderID, customerID == 0 ? null : customerID);
                return Json(result ? new ApiResult(1, "") : new ApiResult(0, "Không thể cập nhật khách hàng."));
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, ex.Message));
            }
        }

        #endregion

        #region Shipper API (dùng cho modal chọn/tạo shipper trong đơn hàng)

        /// <summary>
        /// Lấy tất cả shipper (dùng cho dropdown trong modal)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllShippers()
        {
            var result = await PartnerDataService.ListShippersAsync(new PaginationSearchInput
            {
                Page = 1,
                PageSize = 0,
                SearchValue = ""
            });
            var data = result.DataItems.Select(s => new
            {
                s.ShipperID,
                s.ShipperName,
                s.Phone
            });
            return Json(data);
        }

        /// <summary>
        /// Tạo shipper mới từ modal (trả về ShipperID mới)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateShipperFromModal(string shipperName, string phone)
        {
            if (string.IsNullOrWhiteSpace(shipperName))
                return Json(new { code = 0, message = "Tên người giao hàng không được để trống." });

            try
            {
                var newShipper = new Shipper()
                {
                    ShipperName = shipperName,
                    Phone = phone ?? ""
                };
                int newId = await PartnerDataService.AddShipperAsync(newShipper);
                var created = await PartnerDataService.GetShipperAsync(newId);
                return Json(new
                {
                    code = 1,
                    message = "",
                    data = new
                    {
                        created!.ShipperID,
                        created.ShipperName,
                        created.Phone
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { code = 0, message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin shipper và địa chỉ giao hàng cho đơn hàng (AJAX)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateOrderShipperDelivery(int orderID, int shipperID,
            string deliveryProvince, string deliveryAddress)
        {
            try
            {
                bool result = await SalesDataService.UpdateOrderShipperDeliveryAsync(
                    orderID,
                    shipperID == 0 ? null : shipperID,
                    deliveryProvince ?? "",
                    deliveryAddress ?? "");
                return Json(result ? new ApiResult(1, "") : new ApiResult(0, "Không thể cập nhật thông tin giao hàng."));
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, ex.Message));
            }
        }

        #endregion

        #region Xem và xử lý đơn hàng

        /// <summary>
        /// Xem chi tiết đơn hàng
        /// </summary>
        public async Task<IActionResult> Detail(int id)
        {
            var model = await SalesDataService.GetOrderAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.Details = await SalesDataService.ListDetailsAsync(id);
            ViewBag.Title = $"Chi tiết đơn hàng #{id}";
            return View(model);
        }

        /// <summary>
        /// GET: Hiển thị modal xác nhận duyệt đơn hàng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Accept(int id)
        {
            var model = await SalesDataService.GetOrderAsync(id);
            return PartialView(model);
        }

        /// <summary>
        /// POST: Thực hiện duyệt đơn hàng
        /// </summary>
        [HttpPost]
        [ActionName("Accept")]
        public async Task<IActionResult> AcceptPost(int id)
        {
            // Lấy EmployeeID từ Claims
            var userData = User.GetUserData();
            int employeeID = 0;
            if (userData != null && int.TryParse(userData.UserId, out int parsedId))
                employeeID = parsedId;

            await SalesDataService.AcceptOrderAsync(id, employeeID);
            return RedirectToAction("Detail", new { id });
        }

        /// <summary>
        /// GET: Hiển thị modal chọn người giao hàng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Shipping(int id)
        {
            var model = await SalesDataService.GetOrderAsync(id);
            // Load danh sách shipper vào ViewBag để tránh gọi DB trong view partial
            var shippers = await PartnerDataService.ListShippersAsync(new PaginationSearchInput
            {
                Page = 1,
                PageSize = 0,
                SearchValue = ""
            });
            ViewBag.Shippers = shippers.DataItems;
            return PartialView(model);
        }

        /// <summary>
        /// POST: Thực hiện chuyển đơn hàng sang trạng thái đang giao.
        /// Kiểm tra bắt buộc: khách hàng, shipper, địa chỉ giao hàng.
        /// </summary>
        [HttpPost]
        [ActionName("Shipping")]
        public async Task<IActionResult> ShippingPost(int id, int shipperID)
        {
            try
            {
                await SalesDataService.ShipOrderAsync(id, shipperID);
                return RedirectToAction("Detail", new { id });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Detail", new { id });
            }
        }

        /// <summary>
        /// GET: Hiển thị modal xác nhận hoàn tất đơn hàng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Finish(int id)
        {
            var model = await SalesDataService.GetOrderAsync(id);
            return PartialView(model);
        }

        /// <summary>
        /// POST: Thực hiện hoàn tất đơn hàng
        /// </summary>
        [HttpPost]
        [ActionName("Finish")]
        public async Task<IActionResult> FinishPost(int id)
        {
            await SalesDataService.CompleteOrderAsync(id);
            return RedirectToAction("Detail", new { id });
        }

        /// <summary>
        /// GET: Hiển thị modal xác nhận từ chối đơn hàng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Reject(int id)
        {
            var model = await SalesDataService.GetOrderAsync(id);
            return PartialView(model);
        }

        /// <summary>
        /// POST: Thực hiện từ chối đơn hàng
        /// </summary>
        [HttpPost]
        [ActionName("Reject")]
        public async Task<IActionResult> RejectPost(int id)
        {
            // Lấy EmployeeID từ Claims
            var userData = User.GetUserData();
            int employeeID = 0;
            if (userData != null && int.TryParse(userData.UserId, out int parsedId))
                employeeID = parsedId;

            await SalesDataService.RejectOrderAsync(id, employeeID);
            return RedirectToAction("Detail", new { id });
        }

        /// <summary>
        /// GET: Hiển thị modal xác nhận hủy đơn hàng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Cancel(int id)
        {
            var model = await SalesDataService.GetOrderAsync(id);
            return PartialView(model);
        }

        /// <summary>
        /// POST: Thực hiện hủy đơn hàng
        /// </summary>
        [HttpPost]
        [ActionName("Cancel")]
        public async Task<IActionResult> CancelPost(int id)
        {
            // Lấy EmployeeID từ Claims
            var userData = User.GetUserData();
            int employeeID = 0;
            if (userData != null && int.TryParse(userData.UserId, out int parsedId))
                employeeID = parsedId;

            await SalesDataService.CancelOrderAsync(id, employeeID);
            return RedirectToAction("Detail", new { id });
        }

        /// <summary>
        /// Xóa đơn hàng (GET: modal xác nhận; POST: thực hiện xóa)
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                try { await SalesDataService.DeleteOrderAsync(id); } catch { }
                return RedirectToAction("Index");
            }

            var model = await SalesDataService.GetOrderAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return PartialView(model);
        }

        #endregion

        #region Quản lý mặt hàng trong đơn hàng (Detail view)

        /// <summary>
        /// AJAX: Load danh sách mặt hàng trong đơn hàng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> LoadOrderDetails(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return Content("");

            var details = await SalesDataService.ListDetailsAsync(id);
            ViewBag.OrderID = id;
            ViewBag.OrderStatus = order.Status;
            return PartialView("_OrderDetails", details);
        }

        /// <summary>
        /// GET: Partial view modal tìm và thêm mặt hàng vào đơn hàng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AddOrderItem(int id)
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>("SearchProductInDetail")
                        ?? new ProductSearchInput { Page = 1, PageSize = 5, SearchValue = "" };
            ViewBag.OrderID = id;
            var products = await CatalogDataService.ListProductsAsync(input);
            return PartialView(products);
        }

        /// <summary>
        /// AJAX: Tìm kiếm mặt hàng trong modal thêm vào đơn hàng — trả về partial view
        /// nhận các param rời để tránh model binding bị lẫn
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchProductInDetail(int orderID, string searchValue = "",
            int pageSize = 5, int page = 1)
        {
            var input = new ProductSearchInput
            {
                Page = page,
                PageSize = pageSize,
                SearchValue = searchValue
            };
            ApplicationContext.SetSessionData("SearchProductInDetail", input);
            var result = await CatalogDataService.ListProductsAsync(input);
            ViewBag.OrderID = orderID;
            return PartialView("_SearchProductInDetail", result);
        }

        /// <summary>
        /// POST: Thêm hoặc cập nhật mặt hàng vào đơn hàng
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddOrderItem(int orderID, int productID, int quantity, decimal salePrice)
        {
            if (quantity <= 0)
                return Json(new ApiResult(0, "Số lượng phải lớn hơn 0"));
            if (salePrice < 0)
                return Json(new ApiResult(0, "Giá bán không được âm"));

            try
            {
                var result = await SalesDataService.AddDetailAsync(new OrderDetail()
                {
                    OrderID = orderID,
                    ProductID = productID,
                    Quantity = quantity,
                    SalePrice = salePrice
                });
                return Json(result ? new ApiResult(1, "") : new ApiResult(0, "Không thể thêm mặt hàng"));
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, ex.Message));
            }
        }

        /// <summary>
        /// GET: Partial view form sửa số lượng/giá mặt hàng trong đơn
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditOrderItem(int id, int productId)
        {
            var detail = await SalesDataService.GetDetailAsync(id, productId);
            return PartialView(detail);
        }

        /// <summary>
        /// POST: Cập nhật số lượng và giá bán của mặt hàng trong đơn hàng
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateOrderItem(int orderID, int productID, int quantity, decimal salePrice)
        {
            if (quantity <= 0)
                return Json(new ApiResult(0, "Số lượng phải lớn hơn 0"));
            if (salePrice < 0)
                return Json(new ApiResult(0, "Giá bán không được âm"));

            try
            {
                var result = await SalesDataService.UpdateDetailAsync(new OrderDetail()
                {
                    OrderID = orderID,
                    ProductID = productID,
                    Quantity = quantity,
                    SalePrice = salePrice
                });
                return Json(result ? new ApiResult(1, "") : new ApiResult(0, "Không thể cập nhật mặt hàng"));
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, ex.Message));
            }
        }

        /// <summary>
        /// GET: Partial view xác nhận xóa mặt hàng khỏi đơn hàng
        /// POST: Thực hiện xóa
        /// </summary>
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> DeleteOrderItem(int id, int productId)
        {
            if (Request.Method == "POST")
            {
                try
                {
                    var result = await SalesDataService.DeleteDetailAsync(id, productId);
                    return Json(result ? new ApiResult(1, "") : new ApiResult(0, "Không thể xóa mặt hàng"));
                }
                catch (Exception ex)
                {
                    return Json(new ApiResult(0, ex.Message));
                }
            }

            var detail = await SalesDataService.GetDetailAsync(id, productId);
            ViewBag.OrderID = id;
            return PartialView(detail);
        }

        #endregion
    }
}