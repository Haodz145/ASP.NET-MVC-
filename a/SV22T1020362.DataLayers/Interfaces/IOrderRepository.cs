using SV22T1020362.Models.Common;
using SV22T1020362.Models.Sales;

namespace SV22T1020362.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các chức năng xử lý dữ liệu cho đơn hàng
    /// </summary>
    public interface IOrderRepository
    {
        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng dưới dạng phân trang
        /// </summary>
        Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input);

        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng CẦN DUYỆT dưới dạng phân trang
        /// </summary>
        Task<PagedResult<OrderViewInfo>> ListListStatusAsync(OrderSearchInputListStatus input);

        /// <summary>
        /// Lấy thông tin 1 đơn hàng
        /// </summary>
        Task<OrderViewInfo?> GetAsync(int orderID);

        /// <summary>
        /// Bổ sung đơn hàng
        /// </summary>
        Task<int> AddAsync(Order data);

        /// <summary>
        /// Cập nhật đơn hàng
        /// </summary>
        Task<bool> UpdateAsync(Order data);

        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
        Task<bool> DeleteAsync(int orderID);

        /// <summary>
        /// Cập nhật chỉ thông tin khách hàng cho đơn hàng (CustomerID)
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <param name="customerID">Mã khách hàng (null để xóa)</param>
        Task<bool> UpdateCustomerAsync(int orderID, int? customerID);

        /// <summary>
        /// Cập nhật thông tin giao hàng: shipper, địa chỉ, tỉnh thành
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <param name="shipperID">Mã người giao hàng</param>
        /// <param name="deliveryProvince">Tỉnh/thành giao hàng</param>
        /// <param name="deliveryAddress">Địa chỉ giao hàng</param>
        Task<bool> UpdateShipperDeliveryAsync(int orderID, int? shipperID, string deliveryProvince, string deliveryAddress);

        /// <summary>
        /// Lấy danh sách đơn hàng của một khách hàng cụ thể, sắp xếp mới nhất trước
        /// </summary>
        Task<List<OrderViewInfo>> ListByCustomerAsync(int customerID);

        /// <summary>
        /// Lấy danh sách top N mặt hàng bán chạy nhất theo tổng số lượng trong OrderDetails
        /// </summary>
        /// <param name="top">Số lượng mặt hàng cần lấy</param>
        Task<List<TopSellingProduct>> GetTopSellingProductsAsync(int top = 4);

        /// <summary>
        /// Lấy danh sách mặt hàng trong đơn hàng
        /// </summary>
        Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID);

        /// <summary>
        /// Lấy thông tin chi tiết của một mặt hàng trong một đơn hàng
        /// </summary>
        Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID);

        /// <summary>
        /// Bổ sung mặt hàng vào đơn hàng
        /// </summary>
        Task<bool> AddDetailAsync(OrderDetail data);

        /// <summary>
        /// Cập nhật số lượng và giá bán của một mặt hàng trong đơn hàng
        /// </summary>
        Task<bool> UpdateDetailAsync(OrderDetail data);

        /// <summary>
        /// Xóa một mặt hàng khỏi đơn hàng
        /// </summary>
        Task<bool> DeleteDetailAsync(int orderID, int productID);

        /// <summary>
        /// Đếm số lượng mặt hàng trong đơn hàng
        /// </summary>
        Task<int> CountDetailsAsync(int orderID);

        /// <summary>
        /// Lấy đơn hàng giỏ hàng (Status=0) của khách hàng
        /// </summary>
        Task<OrderViewInfo?> GetCartOrderAsync(int customerID);

        /// <summary>
        /// Lấy hoặc tạo đơn hàng giỏ hàng cho khách hàng
        /// </summary>
        Task<int> GetOrCreateCartAsync(int customerID);

        /// <summary>
        /// Xóa đơn hàng giỏ hàng (Status=0) của khách hàng
        /// </summary>
        Task<bool> DeleteCartAsync(int customerID);

        /// <summary>
        /// Chuyển đơn hàng giỏ hàng (Status=0) thành đơn hàng mới (Status=1)
        /// </summary>
        Task<bool> ConfirmCartAsync(int orderID, string deliveryProvince, string deliveryAddress);
    }
}