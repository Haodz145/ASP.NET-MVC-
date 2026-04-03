namespace SV22T1020362.Models.Sales
{
    /// <summary>
    /// DTO biểu diễn mặt hàng bán chạy (dùng cho thống kê trang chủ Admin)
    /// </summary>
    public class TopSellingProduct
    {
        /// <summary>
        /// Tên mặt hàng
        /// </summary>
        public string ProductName { get; set; } = "";

        /// <summary>
        /// Tổng số lượng đã bán (tính từ OrderDetails)
        /// </summary>
        public int TotalQuantity { get; set; }
    }
}