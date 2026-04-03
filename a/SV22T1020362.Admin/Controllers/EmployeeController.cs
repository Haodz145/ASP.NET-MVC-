using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020362.BusinessLayers;
using SV22T1020362.Models.Common;
using SV22T1020362.Models.HR;

namespace SV22T1020362.Admin.Controllers
{
    /// <summary>
    /// Cung cấp các chức năng liên quan đến nhân viên
    /// // Chỉ Administrator mới được quản lý nhân viên, phân quyền
    /// </summary>
    [Authorize(Roles = WebUserRoles.Administrator)]
    public class EmployeeController : Controller
    {
        public const string SEARCH_EMPLOYEE = "SearchEmployee";

        /// <summary>
        /// Nhập đầu vào tìm kiếm và hiển thị kết quả tìm
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SEARCH_EMPLOYEE);
            if (input == null)
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };
            return View(input);
        }

        /// <summary>
        /// Tìm kiếm và trả về kết quả phân trang
        /// </summary>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await HRDataService.ListEmployeesAsync(input);
            ApplicationContext.SetSessionData(SEARCH_EMPLOYEE, input);
            return View(result);
        }

        /// <summary>
        /// Bổ sung nhân viên mới
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhân viên";
            var model = new Employee() { EmployeeID = 0, IsWorking = true };
            return View("Edit", model);
        }

        /// <summary>
        /// Cập nhật thông tin của nhân viên (không đổi mật khẩu hay phân quyền)
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            return View(model);
        }

        /// <summary>
        /// Lưu dữ liệu nhân viên (bổ sung hoặc cập nhật).
        /// Xử lý upload ảnh đại diện nếu có.
        /// </summary>
        /// <param name="data">Dữ liệu nhân viên từ form</param>
        /// <param name="uploadPhoto">File ảnh được upload (nếu có)</param>
        [HttpPost]
        public async Task<IActionResult> SaveData(Employee data, IFormFile? uploadPhoto)
        {
            try
            {
                ViewBag.Title = data.EmployeeID == 0 ? "Bổ sung nhân viên" : "Cập nhật thông tin nhân viên";

                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrWhiteSpace(data.FullName))
                    ModelState.AddModelError(nameof(data.FullName), "Vui lòng nhập họ tên nhân viên");

                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email nhân viên");
                else if (!await HRDataService.ValidateEmployeeEmailAsync(data.Email, data.EmployeeID))
                    ModelState.AddModelError(nameof(data.Email), "Email đã được sử dụng bởi nhân viên khác");

                if (!ModelState.IsValid)
                    return View("Edit", data);

                // Xử lý upload ảnh đại diện
                if (uploadPhoto != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                    var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images/employees", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;

                    // Đồng bộ ảnh sang Shop
                    ImageSyncHelper.SyncToShop(fileName, "employees");
                }

                // Tiền xử lý dữ liệu
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Photo)) data.Photo = "nophoto.png";

                // Lưu vào database
                if (data.EmployeeID == 0)
                    await HRDataService.AddEmployeeAsync(data);
                else
                    await HRDataService.UpdateEmployeeAsync(data);

                ApplicationContext.SetSessionData(SEARCH_EMPLOYEE, new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = data.Email
                });
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("Edit", data);
            }
        }

        /// <summary>
        /// Hiển thị form đổi mật khẩu của nhân viên.
        /// Lấy thông tin nhân viên để hiển thị trên form.
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            ViewBag.Title = "Đổi mật khẩu nhân viên";
            return View(model);
        }

        /// <summary>
        /// Xử lý đổi mật khẩu cho nhân viên.
        /// Gọi SecurityDataService để cập nhật mật khẩu theo email của nhân viên.
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <param name="newPassword">Mật khẩu mới</param>
        /// <param name="confirmPassword">Xác nhận mật khẩu mới</param>
        [HttpPost]
        public async Task<IActionResult> ChangePassword(int id, string newPassword, string confirmPassword)
        {
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.Title = "Đổi mật khẩu nhân viên";

            if (string.IsNullOrWhiteSpace(newPassword))
                ModelState.AddModelError("newPassword", "Vui lòng nhập mật khẩu mới");
            else if (newPassword.Length < 6)
                ModelState.AddModelError("newPassword", "Mật khẩu phải có ít nhất 6 ký tự");

            if (string.IsNullOrWhiteSpace(confirmPassword))
                ModelState.AddModelError("confirmPassword", "Vui lòng xác nhận mật khẩu mới");
            else if (newPassword != confirmPassword)
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp");

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // Đổi mật khẩu thông qua SecurityDataService theo hướng bảo mật
                await SecurityDataService.ChangeEmployeePasswordAsync(model.Email, CryptHelper.HashMD5(newPassword));
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        /// <summary>
        /// Hiển thị form phân quyền nhân viên.
        /// Truyền danh sách quyền hiện tại của nhân viên qua ViewBag để pre-check checkbox.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ChangeRole(int id)
        {
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.Title = "Phân quyền nhân viên";

            // Lấy RoleNames hiện tại từ DB thông qua EmployeeAccount
            // Employee model không có RoleNames, cần lấy từ bảng Employees trực tiếp
            // Tạm thời lấy qua SecurityDataService (bằng cách đọc từ DB)
            // Để đơn giản, dùng Dapper trực tiếp trong controller hoặc bổ sung method vào HRDataService
            // Ở đây dùng cách đơn giản: lấy thông qua EmployeeRepository đã có
            var currentRoles = await HRDataService.GetEmployeeRoleNamesAsync(model.EmployeeID);
            ViewBag.CurrentRoles = currentRoles;

            return View(model);
        }

        /// <summary>
        /// Lưu phân quyền mới cho nhân viên thông qua SecurityDataService.
        /// Ghi đè toàn bộ quyền cũ bằng danh sách quyền mới được chọn.
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <param name="roleNames">Mảng tên quyền được chọn từ checkbox</param>
        [HttpPost]
        public async Task<IActionResult> ChangeRole(int id, string[] roleNames)
        {
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.Title = "Phân quyền nhân viên";

            try
            {
                // Ghép mảng quyền thành chuỗi phân cách bởi dấu phẩy
                // Ví dụ: ["employee", "admin"] => "employee;admin"
                string roleNamesStr = string.Join(",", roleNames ?? Array.Empty<string>());

                // Cập nhật quyền qua SecurityDataService — dùng Email làm userName
                await SecurityDataService.ChangeEmployeeRoleNamesAsync(model.Email, roleNamesStr);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        /// <summary>
        /// Xóa nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await HRDataService.DeleteEmployeeAsync(id);
                return RedirectToAction("Index");
            }

            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.AllowDelete = !(await HRDataService.IsUsedEmployeeAsync(id));
            return View(model);
        }
    }
}