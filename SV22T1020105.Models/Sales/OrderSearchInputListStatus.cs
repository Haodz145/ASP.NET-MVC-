namespace SV22T1020105.Models.Sales
{
    /// <summary>
    /// Đầu vào tìm kiếm đơn hàng theo danh sách trạng thái
    /// </summary>
    public class OrderSearchInputListStatus : OrderSearchInput
    {
        /// <summary>
        /// Danh sách trạng thái cần lọc
        /// </summary>
        public List<OrderStatusEnum> Statuses { get; set; } = new();
    }
}
