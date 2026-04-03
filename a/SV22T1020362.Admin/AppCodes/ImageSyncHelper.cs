namespace SV22T1020362.Admin
{
    /// <summary>
    /// Tiện ích đồng bộ ảnh giữa Admin và Shop.
    /// Khi upload ảnh ở Admin, tự động copy sang wwwroot tương ứng của Shop.
    /// </summary>
    public static class ImageSyncHelper
    {
        /// <summary>
        /// Copy file ảnh vừa upload sang thư mục tương ứng của project Shop.
        /// </summary>
        /// <param name="fileName">Tên file ảnh (vd: abc123.jpg)</param>
        /// <param name="subFolder">Thư mục con: "employees" hoặc "products"</param>
        public static void SyncToShop(string fileName, string subFolder)
        {
            try
            {
                // Đường dẫn đến wwwroot của Shop (lấy từ appsettings hoặc suy ra từ ContentRoot)
                string shopWwwRoot = ApplicationContext.GetConfigValue("ShopWwwRoot");

                // Nếu không cấu hình trong appsettings, tự suy ra từ ContentRootPath
                if (string.IsNullOrEmpty(shopWwwRoot))
                {
                    // ContentRootPath trỏ đến thư mục project Admin đang chạy
                    // Shop nằm cùng cấp với Admin
                    string contentRoot = ApplicationContext.ApplicationRootPath;
                    string solutionDir = Path.GetFullPath(Path.Combine(contentRoot, ".."));
                    shopWwwRoot = Path.Combine(solutionDir, "SV22T1020362.Shop", "wwwroot");
                }

                string destDir = Path.Combine(shopWwwRoot, "images", subFolder);
                string destFile = Path.Combine(destDir, fileName);

                // Thư mục nguồn trong Admin
                string srcFile = Path.Combine(ApplicationContext.WWWRootPath, "images", subFolder, fileName);

                // Chỉ copy nếu file nguồn tồn tại và thư mục đích tồn tại
                if (File.Exists(srcFile) && Directory.Exists(destDir))
                {
                    File.Copy(srcFile, destFile, overwrite: true);
                }
            }
            catch
            {
                // Không để lỗi sync ảnh làm crash luồng chính
            }
        }
    }
}