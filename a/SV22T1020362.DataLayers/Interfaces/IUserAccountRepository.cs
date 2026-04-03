using SV22T1020362.Models.Security;

namespace SV22T1020362.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu liên quan đến tài khoản
    /// </summary>
    public interface IUserAccountRepository
    {
        /// <summary>
        /// Kiểm tra xem tên đăng nhập và mật khẩu có hợp lệ không
        /// </summary>
        /// <param name="userName">Tên đăng nhập (email)</param>
        /// <param name="password">Mật khẩu</param>
        /// <returns>
        /// Thông tin tài khoản nếu hợp lệ, ngược lại trả về null
        /// </returns>
        Task<UserAccount?> AuthorizeAsync(string userName, string password);

        /// <summary>
        /// Đổi mật khẩu của tài khoản
        /// </summary>
        /// <param name="userName">Tên đăng nhập (email)</param>
        /// <param name="password">Mật khẩu mới</param>
        /// <returns>true nếu đổi mật khẩu thành công</returns>
        Task<bool> ChangePasswordAsync(string userName, string password);

        /// <summary>
        /// Cập nhật danh sách quyền cho tài khoản.
        /// Chỉ áp dụng cho tài khoản nhân viên (Employee).
        /// Tài khoản khách hàng (Customer) không có chức năng phân quyền —
        /// triển khai mặc định trả về false.
        /// </summary>
        /// <param name="userName">Tên đăng nhập (email) của tài khoản cần cập nhật quyền</param>
        /// <param name="roleNames">
        /// Chuỗi tên các quyền phân cách bởi dấu phẩy,
        /// ví dụ: "administrator,sales".
        /// Truyền chuỗi rỗng để xóa toàn bộ quyền.
        /// </param>
        /// <returns>true nếu cập nhật thành công</returns>
        Task<bool> ChangeRoleNamesAsync(string userName, string roleNames);
    }
}