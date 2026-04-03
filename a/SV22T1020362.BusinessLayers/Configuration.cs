using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020362.BusinessLayers
{
    /// <summary>
    /// Khởi tạo và luu trữ các thông tin cấu hình sử dụng cho BusinessLayer
    /// </summary>
    public static class Configuration
    {
        private static string _connectionString = string.Empty;
        /// <summary>
        /// Khởi tạo cấu hình cho Business Layer
        /// (Hàm này phải được gọi trước khi chạy ứng dụng)
        /// </summary>
        /// <param name="connectionString">Chuỗi tham số để kết nối đến CSDL</param>
        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
        }
        /// <summary>
        /// Chuỗi tham số kết nối đến cơ sở dữ liệu
        /// </summary>
        public static string ConnectionString => _connectionString;
    }
}
