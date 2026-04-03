using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020362.BusinessLayers;
using SV22T1020362.Models.Common;
using SV22T1020362.Models.Partner;

namespace SV22T1020362.Admin.Controllers
{
    /// <summary>
    /// Các chức năng liên quan đến khách hàng
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class CustomerController : Controller
    {
        public const string SEARCH_CUSTOMER = "SearchCustomer";
        /// <summary>
        /// Nhập đầu vào tìm kiếm và hiển thị kết quả tìm
        /// </summary>
        /// <param name="page"></param>
        /// <param name="searchValue"></param>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SEARCH_CUSTOMER);
            if (input == null) 
            {
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };
            }
            return View(input);
        }
        /// <summary>
        /// Tìm kiếm và trả về kết quả phân trang
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            //Tìm kiếm
            var result =  await PartnerDataService.ListCustomersAsync(input);

            //Lưu lại điều kiện tìm kiếm vào Session (Ghi nhớ dữ liệu để quay lại trang vẫn còn)
            ApplicationContext.SetSessionData(SEARCH_CUSTOMER, input);

            //Trả về kết quả cho View
            return View(result);
        }
        /// <summary>
        /// Tạo khách hàng mới
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung Khách hàng";
            var model = new Customer()
            {
                CustomerID = 0
            };
            return View("Edit", model);
        }

        /// <summary>
        /// Cập nhật thông tin khách hàng
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.title = "Cập nhật thông tin Khách hàng";
            var model = await PartnerDataService.GetCustomerAsync(id);
            if(model == null)
            {
                return RedirectToAction("Index");
            }
            
            return View(model);
        }

        /// <summary>
        /// Lưu dữ liệu
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> SaveData(Customer data)
        {
            //try
            //{ 
            ViewBag.Title = data.CustomerID == 0 ? "Bổ sung khách hàng" : " Cập nhật thông tin khách hàng";

            //TODO: Kiểm tra dữ liệu đầu vào có hợp lệ hay không?

            //Sử dụng ModelState để kiểm soát lỗi và thông báo lỗi
            if (string.IsNullOrWhiteSpace(data.CustomerName))
            {
                ModelState.AddModelError(nameof(data.CustomerName), "Vui lòng nhập tên khách hàng");
            }
            if (string.IsNullOrWhiteSpace(data.Email))
            {
                ModelState.AddModelError(nameof(data.Email), "Hãy cho biết Email của khách hàng");
            }
            else if (!(await PartnerDataService.ValidateCustomerEmailAsync(data.Email, data.CustomerID)))
            {
                ModelState.AddModelError(nameof(data.Email), "Email này đã có khách hàng sử dụng");
            }    
            if (string.IsNullOrWhiteSpace(data.Province))
            {
                ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn Tỉnh/Thành");
            }

            // Điều chỉnh dữ liệu theo logic/ qui ước của hệ thống
            if (string.IsNullOrEmpty(data.ContactName)) data.ContactName = "";
            if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
            if (string.IsNullOrEmpty(data.Address)) data.Address = "";

            // Nếu có lỗi thì thông báo cho người dùng (qua View), không lưu dữ liệu
            if (!ModelState.IsValid)
            {
                return View("Edit", data);
            }    

            //Lưu dữ liệu vào cơ sở dữ liệu
            if (data.CustomerID == 0)
            {
                await PartnerDataService.AddCustomerAsync(data);
            }
            else
            {
                await PartnerDataService.UpdateCustomerAsync(data);
            }

            PaginationSearchInput input = new PaginationSearchInput()
            {
                Page = 1,
                PageSize = ApplicationContext.PageSize,
                SearchValue = data.Email
            };
            ApplicationContext.SetSessionData(SEARCH_CUSTOMER, input);

            return RedirectToAction("Index");
            //}
            //catch (Exception ex)
            //{
            //    return Content("Hệ thống đang bận, vui lòng thử lại sau.");
            //}
                    
        }

        /// <summary>
        /// Hiển thị form đổi mật khẩu của khách hàng
        /// </summary>
        /// <param name="id">Mã khách hàng</param>
        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            ViewBag.Title = "Đổi mật khẩu khách hàng";
            return View(model);
        }

        /// <summary>
        /// Xử lý đổi mật khẩu cho khách hàng
        /// </summary>
        /// <param name="id">Mã khách hàng</param>
        /// <param name="newPassword">Mật khẩu mới</param>
        /// <param name="confirmPassword">Xác nhận mật khẩu mới</param>
        [HttpPost]
        public async Task<IActionResult> ChangePassword(int id, string newPassword, string confirmPassword)
        {
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.Title = "Đổi mật khẩu khách hàng";

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
                await SecurityDataService.ChangeCustomerPasswordAsync(model.Email, CryptHelper.HashMD5(newPassword));
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        /// <summary>
        /// Xóa khách hàng
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteCustomerAsync(id);
                return RedirectToAction("Index");
            } 
                
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }

            bool allowDelete = !(await PartnerDataService.IsUsedCustomerAsync(id));
            ViewBag.AllowDelete = allowDelete;

            return View(model);
        }
    }
}