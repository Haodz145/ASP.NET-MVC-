using SV22T1020362.DataLayers.Interfaces;
using SV22T1020362.DataLayers.SQLServer;
using SV22T1020362.Models.Common;
using SV22T1020362.Models.HR;
using SV22T1020362.Models.Sales;

namespace SV22T1020362.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến bán hàng
    /// bao gồm: đơn hàng (Order) và chi tiết đơn hàng (OrderDetail).
    /// </summary>
    public static class SalesDataService
    {
        private static readonly IOrderRepository orderDB;

        static SalesDataService()
        {
            orderDB = new OrderRepository(Configuration.ConnectionString);
        }

        #region Order

        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng dưới dạng phân trang
        /// </summary>
        public static async Task<PagedResult<OrderViewInfo>> ListOrdersAsync(OrderSearchInput input)
        {
            return await orderDB.ListAsync(input);
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng CẦN DUYỆT dưới dạng phân trang
        /// </summary>
        public static async Task<PagedResult<OrderViewInfo>> ListOrdersListStatusAsync(OrderSearchInputListStatus input)
        {
            return await orderDB.ListListStatusAsync(input);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một đơn hàng
        /// </summary>
        public static async Task<OrderViewInfo?> GetOrderAsync(int orderID)
        {
            return await orderDB.GetAsync(orderID);
        }

        /// <summary>
        /// Tạo đơn hàng mới.
        /// Status mặc định là New (1), OrderTime là hiện tại.
        /// </summary>
        /// <param name="customerID">Mã khách hàng (0 nếu không có)</param>
        /// <param name="deliveryProvince">Tỉnh/thành giao hàng</param>
        /// <param name="deliveryAddress">Địa chỉ giao hàng</param>
        /// <param name="employeeID">Mã nhân viên tạo đơn (0 nếu không có)</param>
        /// <returns>Mã đơn hàng được tạo mới</returns>
        public static async Task<int> AddOrderAsync(int customerID, string deliveryProvince = "",
            string deliveryAddress = "")
        {
            var order = new Order()
            {
                CustomerID = customerID == 0 ? null : customerID,
                DeliveryProvince = deliveryProvince,
                DeliveryAddress = deliveryAddress,
                OrderTime = DateTime.Now,
                Status = OrderStatusEnum.New
            };
            return await orderDB.AddAsync(order);
        }

        /// <summary>
        /// Cập nhật thông tin đơn hàng (chỉ cho phép khi đang ở trạng thái New)
        /// </summary>
        public static async Task<bool> UpdateOrderAsync(Order data)
        {
            var order = await orderDB.GetAsync(data.OrderID);
            if (order == null)
                return false;
            if (order.Status != OrderStatusEnum.New)
                throw new InvalidOperationException("Chỉ được cập nhật đơn hàng ở trạng thái chờ duyệt.");
            return await orderDB.UpdateAsync(data);
        }

        /// <summary>
        /// Xóa đơn hàng (chỉ cho phép khi đang ở trạng thái New)
        /// </summary>
        public static async Task<bool> DeleteOrderAsync(int orderID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return false;
            if (order.Status != OrderStatusEnum.New)
                throw new InvalidOperationException("Chỉ được xóa đơn hàng ở trạng thái chờ duyệt.");
            return await orderDB.DeleteAsync(orderID);
        }

        /// <summary>
        /// Cập nhật CustomerID cho đơn hàng.
        /// Cho phép ở trạng thái New và Accepted.
        /// </summary>
        public static async Task<bool> UpdateOrderCustomerAsync(int orderID, int? customerID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return false;
            if (order.Status != OrderStatusEnum.New && order.Status != OrderStatusEnum.Accepted)
                throw new InvalidOperationException("Chỉ được cập nhật khách hàng khi đơn hàng ở trạng thái chờ duyệt hoặc đã duyệt.");
            return await orderDB.UpdateCustomerAsync(orderID, customerID);
        }

        /// <summary>
        /// Cập nhật thông tin shipper và địa chỉ giao hàng.
        /// Chỉ cho phép ở trạng thái Accepted.
        /// </summary>
        public static async Task<bool> UpdateOrderShipperDeliveryAsync(int orderID, int? shipperID,
            string deliveryProvince, string deliveryAddress)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return false;
            if (order.Status != OrderStatusEnum.Accepted)
                throw new InvalidOperationException("Chỉ được cập nhật thông tin giao hàng khi đơn hàng đã được duyệt.");
            return await orderDB.UpdateShipperDeliveryAsync(orderID, shipperID, deliveryProvince, deliveryAddress);
        }

        /// <summary>
        /// Lấy danh sách đơn hàng của một khách hàng
        /// </summary>
        public static async Task<List<OrderViewInfo>> ListOrdersByCustomerAsync(int customerID)
        {
            return await orderDB.ListByCustomerAsync(customerID);
        }

        /// <summary>
        /// Lấy danh sách top N mặt hàng bán chạy nhất
        /// </summary>
        /// <param name="top">Số lượng mặt hàng cần lấy (mặc định 4)</param>
        public static async Task<List<TopSellingProduct>> GetTopSellingProductsAsync(int top = 4)
        {
            return await orderDB.GetTopSellingProductsAsync(top);
        }

        #endregion

        #region Order Status Processing

        /// <summary>
        /// Duyệt đơn hàng (New → Accepted)
        /// </summary>
        public static async Task<bool> AcceptOrderAsync(int orderID, int employeeID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null) return false;
            if (order.Status != OrderStatusEnum.New) return false;

            order.EmployeeID = employeeID;
            order.AcceptTime = DateTime.Now;
            order.Status = OrderStatusEnum.Accepted;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Từ chối đơn hàng (New → Rejected)
        /// </summary>
        public static async Task<bool> RejectOrderAsync(int orderID, int employeeID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null) return false;
            if (order.Status != OrderStatusEnum.New) return false;

            order.EmployeeID = employeeID;
            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Rejected;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Hủy đơn hàng (New/Accepted/Shipping → Cancelled)
        /// </summary>
        public static async Task<bool> CancelOrderAsync(int orderID, int employeeID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null) return false;

            if (order.Status != OrderStatusEnum.New &&
                order.Status != OrderStatusEnum.Accepted &&
                order.Status != OrderStatusEnum.Shipping)
                return false;

            order.EmployeeID = employeeID;
            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Cancelled;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Chuyển đơn hàng sang trạng thái đang giao (Accepted → Shipping).
        /// Yêu cầu phải có đầy đủ thông tin khách hàng và giao hàng.
        /// </summary>
        public static async Task<bool> ShipOrderAsync(int orderID, int shipperID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null) return false;
            if (order.Status != OrderStatusEnum.Accepted) return false;

            // Kiểm tra bắt buộc có khách hàng
            if (order.CustomerID == null || order.CustomerID == 0)
                throw new InvalidOperationException("Phải chọn khách hàng trước khi giao hàng.");

            // Kiểm tra bắt buộc có shipper
            if (shipperID == 0)
                throw new InvalidOperationException("Phải chọn người giao hàng trước khi giao hàng.");

            // Kiểm tra bắt buộc có địa chỉ giao hàng
            if (string.IsNullOrWhiteSpace(order.DeliveryProvince) || string.IsNullOrWhiteSpace(order.DeliveryAddress))
                throw new InvalidOperationException("Phải nhập đầy đủ địa chỉ giao hàng trước khi giao hàng.");

            order.ShipperID = shipperID;
            order.ShippedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Shipping;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Hoàn tất đơn hàng (Shipping → Completed)
        /// </summary>
        public static async Task<bool> CompleteOrderAsync(int orderID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null) return false;
            if (order.Status != OrderStatusEnum.Shipping) return false;

            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Completed;

            return await orderDB.UpdateAsync(order);
        }

        #endregion

        #region Order Detail

        /// <summary>
        /// Lấy danh sách mặt hàng của đơn hàng
        /// </summary>
        public static async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            return await orderDB.ListDetailsAsync(orderID);
        }

        /// <summary>
        /// Lấy thông tin một mặt hàng trong đơn hàng
        /// </summary>
        public static async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            return await orderDB.GetDetailAsync(orderID, productID);
        }

        /// <summary>
        /// Thêm mặt hàng vào đơn hàng (chỉ khi trạng thái New)
        /// </summary>
        public static async Task<bool> AddDetailAsync(OrderDetail data)
        {
            var order = await orderDB.GetAsync(data.OrderID);
            if (order == null) return false;
            if (order.Status != OrderStatusEnum.New)
                throw new InvalidOperationException("Chỉ được thêm mặt hàng vào đơn hàng ở trạng thái chờ duyệt.");
            if (data.Quantity <= 0)
                throw new ArgumentException("Số lượng phải lớn hơn 0.");
            if (data.SalePrice < 0)
                throw new ArgumentException("Giá bán không được âm.");
            return await orderDB.AddDetailAsync(data);
        }

        /// <summary>
        /// Cập nhật mặt hàng trong đơn hàng (chỉ khi trạng thái New)
        /// </summary>
        public static async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            var order = await orderDB.GetAsync(data.OrderID);
            if (order == null) return false;
            if (order.Status != OrderStatusEnum.New)
                throw new InvalidOperationException("Chỉ được cập nhật mặt hàng trong đơn hàng ở trạng thái chờ duyệt.");
            if (data.Quantity <= 0)
                throw new ArgumentException("Số lượng phải lớn hơn 0.");
            if (data.SalePrice < 0)
                throw new ArgumentException("Giá bán không được âm.");
            return await orderDB.UpdateDetailAsync(data);
        }

        /// <summary>
        /// Xóa mặt hàng khỏi đơn hàng (chỉ khi trạng thái New, tối thiểu phải còn 1 mặt hàng)
        /// </summary>
        public static async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null) return false;
            if (order.Status != OrderStatusEnum.New)
                throw new InvalidOperationException("Chỉ được xóa mặt hàng khỏi đơn hàng ở trạng thái chờ duyệt.");

            // Kiểm tra tối thiểu 1 mặt hàng
            int count = await orderDB.CountDetailsAsync(orderID);
            if (count <= 1)
                throw new InvalidOperationException("Đơn hàng phải có ít nhất 1 mặt hàng. Không thể xóa mặt hàng cuối cùng.");

            return await orderDB.DeleteDetailAsync(orderID, productID);
        }

        #endregion

        #region Cart (Giỏ hàng lưu trong CSDL với Status = 0)

        /// <summary>
        /// Lấy đơn hàng giỏ hàng của khách hàng (Status=0)
        /// </summary>
        public static async Task<OrderViewInfo?> GetCartOrderAsync(int customerID)
        {
            return await orderDB.GetCartOrderAsync(customerID);
        }

        /// <summary>
        /// Lấy OrderID của giỏ hàng, tạo mới nếu chưa có
        /// </summary>
        public static async Task<int> GetOrCreateCartAsync(int customerID)
        {
            return await orderDB.GetOrCreateCartAsync(customerID);
        }

        /// <summary>
        /// Xóa giỏ hàng (Status=0) của khách hàng khỏi CSDL
        /// </summary>
        public static async Task<bool> DeleteCartAsync(int customerID)
        {
            return await orderDB.DeleteCartAsync(customerID);
        }

        /// <summary>
        /// Đặt hàng: chuyển giỏ hàng (Status=0) thành đơn hàng thực (Status=1)
        /// </summary>
        public static async Task<bool> ConfirmCartAsync(int orderID, string deliveryProvince, string deliveryAddress)
        {
            return await orderDB.ConfirmCartAsync(orderID, deliveryProvince, deliveryAddress);
        }

        /// <summary>
        /// Thêm hoặc cập nhật mặt hàng vào giỏ hàng CSDL (Status=0).
        /// Tự động tạo giỏ hàng nếu chưa có.
        /// </summary>
        public static async Task<bool> AddItemToCartDBAsync(int customerID, int productID, int quantity, decimal salePrice)
        {
            int cartOrderID = await orderDB.GetOrCreateCartAsync(customerID);
            var detail = new OrderDetail
            {
                OrderID = cartOrderID,
                ProductID = productID,
                Quantity = quantity,
                SalePrice = salePrice
            };
            return await orderDB.AddDetailAsync(detail);
        }

        /// <summary>
        /// Lấy danh sách mặt hàng trong giỏ hàng CSDL của khách hàng
        /// </summary>
        public static async Task<List<OrderDetailViewInfo>> GetCartItemsFromDBAsync(int customerID)
        {
            var cart = await orderDB.GetCartOrderAsync(customerID);
            if (cart == null) return new List<OrderDetailViewInfo>();
            return await orderDB.ListDetailsAsync(cart.OrderID);
        }

        /// <summary>
        /// Xóa 1 mặt hàng khỏi giỏ hàng CSDL
        /// </summary>
        public static async Task<bool> RemoveCartItemFromDBAsync(int customerID, int productID)
        {
            var cart = await orderDB.GetCartOrderAsync(customerID);
            if (cart == null) return false;
            return await orderDB.DeleteDetailAsync(cart.OrderID, productID);
        }

        /// <summary>
        /// Cập nhật số lượng và giá bán của mặt hàng trong giỏ hàng CSDL
        /// </summary>
        public static async Task<bool> UpdateCartItemInDBAsync(int customerID, int productID, int quantity, decimal salePrice)
        {
            var cart = await orderDB.GetCartOrderAsync(customerID);
            if (cart == null) return false;
            var detail = new OrderDetail
            {
                OrderID = cart.OrderID,
                ProductID = productID,
                Quantity = quantity,
                SalePrice = salePrice
            };
            return await orderDB.UpdateDetailAsync(detail);
        }

        /// <summary>
        /// Merge giỏ hàng Session (khi chưa đăng nhập) vào giỏ hàng CSDL (sau khi đăng nhập)
        /// </summary>
        public static async Task MergeSessionCartToDBAsync(int customerID,
            List<OrderDetailViewInfo> sessionItems)
        {
            if (sessionItems == null || !sessionItems.Any()) return;

            int cartOrderID = await orderDB.GetOrCreateCartAsync(customerID);
            foreach (var item in sessionItems)
            {
                var detail = new OrderDetail
                {
                    OrderID = cartOrderID,
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    SalePrice = item.SalePrice
                };
                await orderDB.AddDetailAsync(detail); // AddDetailAsync đã xử lý IF EXISTS UPDATE ELSE INSERT
            }
        }

        #endregion

    }
}