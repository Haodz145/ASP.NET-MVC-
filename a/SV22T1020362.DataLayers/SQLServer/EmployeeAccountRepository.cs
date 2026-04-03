using Dapper;
using SV22T1020362.DataLayers.Interfaces;
using SV22T1020362.Models.Security;

namespace SV22T1020362.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu liên quan đến tài khoản nhân viên (đăng nhập Admin)
    /// trên SQL Server
    /// </summary>
    public class EmployeeAccountRepository : BaseRepository, IUserAccountRepository
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL</param>
        public EmployeeAccountRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Xác thực thông tin đăng nhập của nhân viên.
        /// Trả về thông tin tài khoản nếu email và mật khẩu hợp lệ,
        /// ngược lại trả về null
        /// </summary>
        /// <param name="userName">Email đăng nhập của nhân viên</param>
        /// <param name="password">Mật khẩu</param>
        /// <returns>Thông tin tài khoản hoặc null nếu không hợp lệ hoặc nhân viên đã nghỉ việc</returns>
        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            using var connection = GetConnection();
            var sql = @"SELECT CAST(EmployeeID AS nvarchar(20)) AS UserId,
                               Email        AS UserName,
                               FullName     AS DisplayName,
                               Email,
                               ISNULL(Photo,     N'') AS Photo,
                               ISNULL(RoleNames, N'') AS RoleNames
                        FROM   Employees
                        WHERE  Email     = @userName
                          AND  Password  = @password
                          AND  IsWorking = 1";
            return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new { userName, password });
        }

        /// <summary>
        /// Đổi mật khẩu cho tài khoản nhân viên có email là userName
        /// </summary>
        /// <param name="userName">Email của nhân viên cần đổi mật khẩu</param>
        /// <param name="password">Mật khẩu mới</param>
        /// <returns>true nếu đổi mật khẩu thành công</returns>
        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using var connection = GetConnection();
            var sql = "UPDATE Employees SET Password = @password WHERE Email = @userName";
            int rows = await connection.ExecuteAsync(sql, new { userName, password });
            return rows > 0;
        }

        /// <summary>
        /// Cập nhật danh sách quyền (RoleNames) cho nhân viên có email là userName.
        /// Chỉ cập nhật duy nhất cột RoleNames trong bảng Employees,
        /// không ảnh hưởng đến các cột thông tin khác.
        /// </summary>
        /// <param name="userName">Email của nhân viên cần cập nhật quyền</param>
        /// <param name="roleNames">
        /// Chuỗi tên các quyền phân cách bởi dấu chấm phẩy,
        /// ví dụ: "Employees;Orders;Products".
        /// Truyền chuỗi rỗng để xóa toàn bộ quyền.
        /// </param>
        /// <returns>true nếu cập nhật thành công</returns>
        public async Task<bool> ChangeRoleNamesAsync(string userName, string roleNames)
        {
            using var connection = GetConnection();
            var sql = "UPDATE Employees SET RoleNames = @roleNames WHERE Email = @userName";
            int rows = await connection.ExecuteAsync(sql, new { userName, roleNames });
            return rows > 0;
        }
    }
}