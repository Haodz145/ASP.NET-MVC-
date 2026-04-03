using Dapper;
using SV22T1020362.DataLayers.Interfaces;
using SV22T1020362.Models.Catalog;
using SV22T1020362.Models.Common;
using SV22T1020362.Models.Sales;

namespace SV22T1020362.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu liên quan đến đơn hàng trên SQL Server,
    /// bao gồm cả chi tiết đơn hàng (OrderDetails)
    /// </summary>
    public class OrderRepository : BaseRepository, IOrderRepository
    {
        /// <summary>
        /// Ctor
        /// </summary>
        public OrderRepository(string connectionString) : base(connectionString)
        {
        }

        #region Order

        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng dưới dạng phân trang
        /// </summary>
        public async Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input)
        {
            using var connection = GetConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@searchValue", $"%{input.SearchValue}%");
            parameters.Add("@status", (int)input.Status == 0 ? null : (int?)input.Status);
            parameters.Add("@dateFrom", input.DateFrom);
            parameters.Add("@dateTo", input.DateTo);

            int rowCount;
            List<OrderViewInfo> data;

            if (input.PageSize == 0)
            {
                var sql = @"SELECT COUNT(*)
                        FROM   Orders o
                               LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                        WHERE  (@searchValue = '%%' OR c.CustomerName LIKE @searchValue OR c.Phone LIKE @searchValue)
                          AND  (@status   IS NULL OR o.Status     = @status)
                          AND  (@dateFrom IS NULL OR o.OrderTime >= @dateFrom)
                          AND  (@dateTo   IS NULL OR o.OrderTime <= DATEADD(day, 1, @dateTo))
                          AND  (o.Status != 0);

                        SELECT o.OrderID,
                               o.CustomerID,
                               o.OrderTime,
                               o.DeliveryProvince,
                               o.DeliveryAddress,
                               o.EmployeeID,
                               o.AcceptTime,
                               o.ShipperID,
                               o.ShippedTime,
                               o.FinishedTime,
                               o.Status,
                               ISNULL(c.CustomerName, N'') AS CustomerName,
                               ISNULL(c.Phone,        N'') AS CustomerPhone,
                               ISNULL(e.FullName,     N'') AS EmployeeName,
                               ISNULL((SELECT SUM(d.Quantity * d.SalePrice)
                                       FROM   OrderDetails d
                                       WHERE  d.OrderID = o.OrderID), 0) AS SumOfPrice
                        FROM   Orders o
                               LEFT JOIN Customers c  ON o.CustomerID = c.CustomerID
                               LEFT JOIN Employees e  ON o.EmployeeID = e.EmployeeID
                        WHERE  (@searchValue = '%%' OR c.CustomerName LIKE @searchValue OR c.Phone LIKE @searchValue)
                          AND  (@status   IS NULL OR o.Status     = @status)
                          AND  (@dateFrom IS NULL OR o.OrderTime >= @dateFrom)
                          AND  (@dateTo   IS NULL OR o.OrderTime <= DATEADD(day, 1, @dateTo))
                          AND  (o.Status != 0)
                        ORDER  BY o.OrderTime DESC;";

                using var multi = await connection.QueryMultipleAsync(sql, parameters);
                rowCount = await multi.ReadFirstAsync<int>();
                data = (await multi.ReadAsync<OrderViewInfo>()).ToList();
            }
            else
            {
                parameters.Add("@pageSize", input.PageSize);
                parameters.Add("@offset", input.Offset);

                var sql = @"SELECT COUNT(*)
                        FROM   Orders o
                               LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                        WHERE  (@searchValue = '%%' OR c.CustomerName LIKE @searchValue OR c.Phone LIKE @searchValue)
                          AND  (@status   IS NULL OR o.Status     = @status)
                          AND  (@dateFrom IS NULL OR o.OrderTime >= @dateFrom)
                          AND  (@dateTo   IS NULL OR o.OrderTime <= DATEADD(day, 1, @dateTo))
                          AND  (o.Status != 0);

                        SELECT o.OrderID,
                               o.CustomerID,
                               o.OrderTime,
                               o.DeliveryProvince,
                               o.DeliveryAddress,
                               o.EmployeeID,
                               o.AcceptTime,
                               o.ShipperID,
                               o.ShippedTime,
                               o.FinishedTime,
                               o.Status,
                               ISNULL(c.CustomerName, N'') AS CustomerName,
                               ISNULL(c.Phone,        N'') AS CustomerPhone,
                               ISNULL(e.FullName,     N'') AS EmployeeName,
                               ISNULL((SELECT SUM(d.Quantity * d.SalePrice)
                                       FROM   OrderDetails d
                                       WHERE  d.OrderID = o.OrderID), 0) AS SumOfPrice
                        FROM   Orders o
                               LEFT JOIN Customers c  ON o.CustomerID = c.CustomerID
                               LEFT JOIN Employees e  ON o.EmployeeID = e.EmployeeID
                        WHERE  (@searchValue = '%%' OR c.CustomerName LIKE @searchValue OR c.Phone LIKE @searchValue)
                          AND  (@status   IS NULL OR o.Status     = @status)
                          AND  (@dateFrom IS NULL OR o.OrderTime >= @dateFrom)
                          AND  (@dateTo   IS NULL OR o.OrderTime <= DATEADD(day, 1, @dateTo))
                          AND  (o.Status != 0)
                        ORDER  BY o.OrderTime DESC
                        OFFSET @offset ROWS
                        FETCH  NEXT @pageSize ROWS ONLY;";

                using var multi = await connection.QueryMultipleAsync(sql, parameters);
                rowCount = await multi.ReadFirstAsync<int>();
                data = (await multi.ReadAsync<OrderViewInfo>()).ToList();
            }

            return new PagedResult<OrderViewInfo>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = data
            };
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng CẦN DUYỆT dưới dạng phân trang
        /// </summary>
        public async Task<PagedResult<OrderViewInfo>> ListListStatusAsync(OrderSearchInputListStatus input)
        {
            using var connection = GetConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@searchValue", $"%{input.SearchValue}%");
            parameters.Add("@statuses", input.Statuses);
            parameters.Add("@dateFrom", input.DateFrom);
            parameters.Add("@dateTo", input.DateTo);

            int rowCount;
            List<OrderViewInfo> data;

            parameters.Add("@pageSize", input.PageSize);
            parameters.Add("@offset", input.Offset);

            var sql = @"SELECT COUNT(*)
                    FROM   Orders o
                            LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                    WHERE  (@searchValue = '%%' OR c.CustomerName LIKE @searchValue OR c.Phone LIKE @searchValue)
                        AND  (o.Status IN @statuses)
                        AND  (@dateFrom IS NULL OR o.OrderTime >= @dateFrom)
                        AND  (@dateTo   IS NULL OR o.OrderTime <= DATEADD(day, 1, @dateTo))
                        AND  (o.Status != 0);

                    SELECT o.OrderID,
                            o.CustomerID,
                            o.OrderTime,
                            o.DeliveryProvince,
                            o.DeliveryAddress,
                            o.EmployeeID,
                            o.AcceptTime,
                            o.ShipperID,
                            o.ShippedTime,
                            o.FinishedTime,
                            o.Status,
                            ISNULL(c.CustomerName, N'') AS CustomerName,
                            ISNULL(c.Phone,        N'') AS CustomerPhone,
                            ISNULL(e.FullName,     N'') AS EmployeeName,
                            ISNULL((SELECT SUM(d.Quantity * d.SalePrice)
                                    FROM   OrderDetails d
                                    WHERE  d.OrderID = o.OrderID), 0) AS SumOfPrice
                    FROM   Orders o
                            LEFT JOIN Customers c  ON o.CustomerID = c.CustomerID
                            LEFT JOIN Employees e  ON o.EmployeeID = e.EmployeeID
                    WHERE  (@searchValue = '%%' OR c.CustomerName LIKE @searchValue OR c.Phone LIKE @searchValue)
                        AND  (o.Status IN @statuses)
                        AND  (@dateFrom IS NULL OR o.OrderTime >= @dateFrom)
                        AND  (@dateTo   IS NULL OR o.OrderTime <= DATEADD(day, 1, @dateTo))
                        AND  (o.Status != 0)
                    ORDER  BY o.OrderTime DESC
                    OFFSET @offset ROWS
                    FETCH  NEXT @pageSize ROWS ONLY;";

            using var multi = await connection.QueryMultipleAsync(sql, parameters);
            rowCount = await multi.ReadFirstAsync<int>();
            data = (await multi.ReadAsync<OrderViewInfo>()).ToList();
            

            return new PagedResult<OrderViewInfo>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = data
            };
        }

        /// <summary>
        /// Lấy thông tin chi tiết của 1 đơn hàng
        /// </summary>
        public async Task<OrderViewInfo?> GetAsync(int orderID)
        {
            using var connection = GetConnection();
            var sql = @"SELECT o.OrderID,
                               o.CustomerID,
                               o.OrderTime,
                               o.DeliveryProvince,
                               o.DeliveryAddress,
                               o.EmployeeID,
                               o.AcceptTime,
                               o.ShipperID,
                               o.ShippedTime,
                               o.FinishedTime,
                               o.Status,
                               ISNULL(e.FullName,          N'') AS EmployeeName,
                               ISNULL(c.CustomerName,      N'') AS CustomerName,
                               ISNULL(c.ContactName,       N'') AS CustomerContactName,
                               ISNULL(c.Email,             N'') AS CustomerEmail,
                               ISNULL(c.Phone,             N'') AS CustomerPhone,
                               ISNULL(c.Address,           N'') AS CustomerAddress,
                               ISNULL(s.ShipperName,       N'') AS ShipperName,
                               ISNULL(s.Phone,             N'') AS ShipperPhone
                        FROM   Orders o
                               LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                               LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                               LEFT JOIN Shippers  s ON o.ShipperID  = s.ShipperID
                        WHERE  o.OrderID = @orderID";
            return await connection.QueryFirstOrDefaultAsync<OrderViewInfo>(sql, new { orderID });
        }

        /// <summary>
        /// Bổ sung một đơn hàng mới
        /// </summary>
        public async Task<int> AddAsync(Order data)
        {
            using var connection = GetConnection();
            var sql = @"INSERT INTO Orders (CustomerID, OrderTime, DeliveryProvince, DeliveryAddress,
                                            EmployeeID, AcceptTime, ShipperID, ShippedTime, FinishedTime, Status)
                        VALUES (@CustomerID, @OrderTime, @DeliveryProvince, @DeliveryAddress,
                                @EmployeeID, @AcceptTime, @ShipperID, @ShippedTime, @FinishedTime, @Status);
                        SELECT SCOPE_IDENTITY();";
            var result = await connection.ExecuteScalarAsync<decimal>(sql, data);
            return (int)result;
        }

        /// <summary>
        /// Cập nhật thông tin đơn hàng
        /// </summary>
        public async Task<bool> UpdateAsync(Order data)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE Orders
                        SET CustomerID       = @CustomerID,
                            OrderTime        = @OrderTime,
                            DeliveryProvince = @DeliveryProvince,
                            DeliveryAddress  = @DeliveryAddress,
                            EmployeeID       = @EmployeeID,
                            AcceptTime       = @AcceptTime,
                            ShipperID        = @ShipperID,
                            ShippedTime      = @ShippedTime,
                            FinishedTime     = @FinishedTime,
                            Status           = @Status
                        WHERE OrderID = @OrderID";
            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa đơn hàng (bao gồm chi tiết)
        /// </summary>
        public async Task<bool> DeleteAsync(int orderID)
        {
            using var connection = GetConnection();
            var sql = "DELETE FROM Orders WHERE OrderID = @orderID";
            int rows = await connection.ExecuteAsync(sql, new { orderID });
            return rows > 0;
        }

        /// <summary>
        /// Cập nhật chỉ CustomerID cho đơn hàng
        /// </summary>
        public async Task<bool> UpdateCustomerAsync(int orderID, int? customerID)
        {
            using var connection = GetConnection();
            var sql = "UPDATE Orders SET CustomerID = @customerID WHERE OrderID = @orderID";
            int rows = await connection.ExecuteAsync(sql, new { orderID, customerID });
            return rows > 0;
        }

        /// <summary>
        /// Cập nhật thông tin shipper và địa chỉ giao hàng cho đơn hàng
        /// </summary>
        public async Task<bool> UpdateShipperDeliveryAsync(int orderID, int? shipperID, string deliveryProvince, string deliveryAddress)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE Orders
                        SET ShipperID        = @shipperID,
                            DeliveryProvince = @deliveryProvince,
                            DeliveryAddress  = @deliveryAddress
                        WHERE OrderID = @orderID";
            int rows = await connection.ExecuteAsync(sql, new { orderID, shipperID, deliveryProvince, deliveryAddress });
            return rows > 0;
        }

        /// <summary>
        /// Lấy danh sách đơn hàng của một khách hàng
        /// </summary>
        public async Task<List<OrderViewInfo>> ListByCustomerAsync(int customerID)
        {
            using var connection = GetConnection();
            var sql = @"SELECT o.OrderID,
                           o.CustomerID,
                           o.OrderTime,
                           o.DeliveryProvince,
                           o.DeliveryAddress,
                           o.EmployeeID,
                           o.AcceptTime,
                           o.ShipperID,
                           o.ShippedTime,
                           o.FinishedTime,
                           o.Status,
                           ISNULL(c.CustomerName,  N'') AS CustomerName,
                           ISNULL(c.Phone,         N'') AS CustomerPhone,
                           ISNULL(e.FullName,      N'') AS EmployeeName,
                           ISNULL((SELECT SUM(d.Quantity * d.SalePrice)
                                   FROM   OrderDetails d
                                   WHERE  d.OrderID = o.OrderID), 0) AS SumOfPrice
                    FROM   Orders o
                           LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                           LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                    WHERE  o.CustomerID = @customerID
                       AND o.Status != 0
                    ORDER  BY o.OrderTime DESC";
            var data = await connection.QueryAsync<OrderViewInfo>(sql, new { customerID });
            return data.ToList();
        }

        /// <summary>
        /// Lấy đơn hàng giỏ hàng (Status=0) của khách hàng
        /// </summary>
        public async Task<OrderViewInfo?> GetCartOrderAsync(int customerID)
        {
            using var connection = GetConnection();
            var sql = @"SELECT o.OrderID,
                               o.CustomerID,
                               o.OrderTime,
                               o.DeliveryProvince,
                               o.DeliveryAddress,
                               o.EmployeeID,
                               o.AcceptTime,
                               o.ShipperID,
                               o.ShippedTime,
                               o.FinishedTime,
                               o.Status,
                               ISNULL(c.CustomerName,  N'') AS CustomerName,
                               ISNULL(c.Phone,         N'') AS CustomerPhone,
                               ISNULL(e.FullName,      N'') AS EmployeeName,
                               ISNULL((SELECT SUM(d.Quantity * d.SalePrice)
                                       FROM   OrderDetails d
                                       WHERE  d.OrderID = o.OrderID), 0) AS SumOfPrice
                        FROM   Orders o
                               LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                               LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                        WHERE  o.CustomerID = @customerID
                          AND  o.Status = 0";
            return await connection.QueryFirstOrDefaultAsync<OrderViewInfo>(sql, new { customerID });
        }

        /// <summary>
        /// Lấy hoặc tạo đơn hàng giỏ hàng (Status=0) cho khách hàng.
        /// Nếu đã có đơn giỏ hàng thì trả về OrderID đó,
        /// nếu chưa có thì tạo mới và trả về OrderID mới.
        /// </summary>
        public async Task<int> GetOrCreateCartAsync(int customerID)
        {
            using var connection = GetConnection();
            // Kiểm tra đã có giỏ hàng chưa
            var existSql = "SELECT OrderID FROM Orders WHERE CustomerID = @customerID AND Status = 0";
            var existID = await connection.ExecuteScalarAsync<int?>(existSql, new { customerID });
            if (existID.HasValue && existID.Value > 0)
                return existID.Value;

            // Tạo đơn hàng giỏ hàng mới
            var insertSql = @"INSERT INTO Orders (CustomerID, OrderTime, DeliveryProvince, DeliveryAddress,
                                                   EmployeeID, AcceptTime, ShipperID, ShippedTime, FinishedTime, Status)
                              VALUES (@customerID, @now, '', '', NULL, NULL, NULL, NULL, NULL, 0);
                              SELECT SCOPE_IDENTITY();";
            var result = await connection.ExecuteScalarAsync<decimal>(insertSql,
                new { customerID, now = DateTime.Now });
            return (int)result;
        }

        /// <summary>
        /// Xóa đơn hàng giỏ hàng (Status=0) và toàn bộ OrderDetails của khách hàng
        /// </summary>
        public async Task<bool> DeleteCartAsync(int customerID)
        {
            using var connection = GetConnection();
            // Lấy OrderID của giỏ hàng
            var orderID = await connection.ExecuteScalarAsync<int?>(
                "SELECT OrderID FROM Orders WHERE CustomerID = @customerID AND Status = 0",
                new { customerID });

            if (!orderID.HasValue) return true; // Không có giỏ hàng → coi như xóa thành công

            // Xóa OrderDetails và Orders (OrderDetails sẽ xóa cascade)
            var sql = "DELETE FROM Orders WHERE OrderID = @orderID AND Status = 0";
            int rows = await connection.ExecuteAsync(sql, new { orderID });
            return rows > 0;
        }

        /// <summary>
        /// Chuyển đơn hàng giỏ hàng (Status=0) thành đơn hàng thực (Status=1),
        /// cập nhật địa chỉ giao hàng và thời gian đặt hàng
        /// </summary>
        public async Task<bool> ConfirmCartAsync(int orderID, string deliveryProvince, string deliveryAddress)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE Orders
                        SET Status           = 1,
                            OrderTime        = @now,
                            DeliveryProvince = @deliveryProvince,
                            DeliveryAddress  = @deliveryAddress
                        WHERE OrderID = @orderID AND Status = 0";
            int rows = await connection.ExecuteAsync(sql, new
            {
                orderID,
                now = DateTime.Now,
                deliveryProvince,
                deliveryAddress
            });
            return rows > 0;
        }

        /// <summary>
        /// Lấy top N mặt hàng bán chạy nhất theo tổng số lượng bán trong OrderDetails
        /// Chỉ tính đối với các đơn hàng đã hoàn tất (Status.Completed = 4)
        /// </summary>
        /// <param name="top">Số lượng mặt hàng muốn lấy</param>
        public async Task<List<TopSellingProduct>> GetTopSellingProductsAsync(int top = 4)
        {
            using var connection = GetConnection();
            var sql = @"SELECT TOP (@top)
                       p.ProductName,
                       SUM(od.Quantity) AS TotalQuantity
                FROM   OrderDetails od
                       JOIN Products p ON od.ProductID = p.ProductID
                       JOIN Orders   o ON od.OrderID   = o.OrderID
                WHERE  o.Status = 4
                GROUP  BY p.ProductName
                ORDER  BY TotalQuantity DESC";
            var data = await connection.QueryAsync<TopSellingProduct>(sql, new { top });
            return data.ToList();
        }

        #endregion

        #region OrderDetails

        /// <summary>
        /// Lấy danh sách mặt hàng trong đơn hàng
        /// </summary>
        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            using var connection = GetConnection();
            var sql = @"SELECT d.OrderID,
                               d.ProductID,
                               d.Quantity,
                               d.SalePrice,
                               ISNULL(p.ProductName, N'') AS ProductName,
                               ISNULL(p.Unit,        N'') AS Unit,
                               ISNULL(p.Photo,       N'') AS Photo
                        FROM   OrderDetails d
                               LEFT JOIN Products p ON d.ProductID = p.ProductID
                        WHERE  d.OrderID = @orderID
                        ORDER  BY p.ProductName";
            var data = await connection.QueryAsync<OrderDetailViewInfo>(sql, new { orderID });
            return data.ToList();
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một mặt hàng trong đơn hàng
        /// </summary>
        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            using var connection = GetConnection();
            var sql = @"SELECT d.OrderID,
                               d.ProductID,
                               d.Quantity,
                               d.SalePrice,
                               ISNULL(p.ProductName, N'') AS ProductName,
                               ISNULL(p.Unit,        N'') AS Unit,
                               ISNULL(p.Photo,       N'') AS Photo
                        FROM   OrderDetails d
                               LEFT JOIN Products p ON d.ProductID = p.ProductID
                        WHERE  d.OrderID   = @orderID
                          AND  d.ProductID = @productID";
            return await connection.QueryFirstOrDefaultAsync<OrderDetailViewInfo>(sql, new { orderID, productID });
        }

        /// <summary>
        /// Bổ sung hoặc cập nhật mặt hàng vào đơn hàng
        /// </summary>
        public async Task<bool> AddDetailAsync(OrderDetail data)
        {
            using var connection = GetConnection();
            var sql = @"IF EXISTS (SELECT 1 FROM OrderDetails WHERE OrderID = @OrderID AND ProductID = @ProductID)
                            UPDATE OrderDetails
                            SET    Quantity  = @Quantity,
                                   SalePrice = @SalePrice
                            WHERE  OrderID   = @OrderID
                              AND  ProductID = @ProductID
                        ELSE
                            INSERT INTO OrderDetails (OrderID, ProductID, Quantity, SalePrice)
                            VALUES (@OrderID, @ProductID, @Quantity, @SalePrice)";
            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Cập nhật số lượng và giá bán
        /// </summary>
        public async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE OrderDetails
                        SET Quantity  = @Quantity,
                            SalePrice = @SalePrice
                        WHERE OrderID   = @OrderID
                          AND ProductID = @ProductID";
            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa một mặt hàng khỏi đơn hàng
        /// </summary>
        public async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            using var connection = GetConnection();
            var sql = "DELETE FROM OrderDetails WHERE OrderID = @orderID AND ProductID = @productID";
            int rows = await connection.ExecuteAsync(sql, new { orderID, productID });
            return rows > 0;
        }

        /// <summary>
        /// Đếm số lượng mặt hàng trong đơn hàng
        /// </summary>
        public async Task<int> CountDetailsAsync(int orderID)
        {
            using var connection = GetConnection();
            var sql = "SELECT COUNT(*) FROM OrderDetails WHERE OrderID = @orderID";
            return await connection.ExecuteScalarAsync<int>(sql, new { orderID });
        }

        #endregion
    }
}