using SV22T1020362.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020362.Models.Sales
{
    /// <summary>
    /// Đầu vào phân trang, dùng để hiển thị danh sách đơn hàng cần duyệt
    /// Dùng cho trang chủ
    /// </summary>
    public class OrderSearchInputListStatus: PaginationSearchInput
    {
        /// <summary>
        /// Danh sách trạng thái đơn hàng của các đơn hàng cần duyệt
        /// </summary>
        public List<OrderStatusEnum> Statuses { get; set; } = [];
        /// <summary>
        /// Từ ngày (ngày lập đơn hàng)
        /// </summary>
        public DateTime? DateFrom { get; set; }
        /// <summary>
        /// Đến ngày (ngày lập đơn hàng)
        /// </summary>
        public DateTime? DateTo { get; set; }
    }
}
