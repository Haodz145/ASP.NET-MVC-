using Dapper;
using SV22T1020362.DataLayers.Interfaces;
using SV22T1020362.Models.Security;

namespace SV22T1020362.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu liên quan đến tài khoản khách hàng (đăng nhập Shop)
    /// trên SQL Server
    /// </summary>
    public class CustomerAccountRepository : BaseRepository, IUserAccountRepository
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL</param>
        public CustomerAccountRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Xác thực thông tin đăng nhập của khách hàng.
        /// Trả về thông tin tài khoản nếu email và mật khẩu hợp lệ,
        /// ngược lại trả về null
        /// </summary>
        /// <param name="userName">Email đăng nhập của khách hàng</param>
        /// <param name="password">Mật khẩu</param>
        /// <returns>Thông tin tài khoản hoặc null nếu không hợp lệ hoặc khách hàng đang bị khóa</returns>
        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            using var connection = GetConnection();
            var sql = @"SELECT CAST(CustomerID AS nvarchar(20)) AS UserId,
                               Email         AS UserName,
                               CustomerName  AS DisplayName,
                               Email,
                               N''           AS Photo,
                               N''           AS RoleNames
                        FROM   Customers
                        WHERE  Email     = @userName
                          AND  Password  = @password
                          AND  IsLocked  = 0";
            return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new { userName, password });
        }

        /// <summary>
        /// Đổi mật khẩu cho tài khoản khách hàng có email là userName
        /// </summary>
        /// <param name="userName">Email của khách hàng cần đổi mật khẩu</param>
        /// <param name="password">Mật khẩu mới</param>
        /// <returns>true nếu đổi mật khẩu thành công</returns>
        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using var connection = GetConnection();
            var sql = "UPDATE Customers SET Password = @password WHERE Email = @userName";
            int rows = await connection.ExecuteAsync(sql, new { userName, password });
            return rows > 0;
        }

        /// <summary>
        /// Khách hàng không có chức năng phân quyền.
        /// Phương thức này không thực hiện thao tác nào và luôn trả về false.
        /// </summary>
        /// <param name="userName">Không sử dụng</param>
        /// <param name="roleNames">Không sử dụng</param>
        /// <returns>Luôn trả về false</returns>
        public Task<bool> ChangeRoleNamesAsync(string userName, string roleNames)
        {
            return Task.FromResult(false);
        }
    }
}