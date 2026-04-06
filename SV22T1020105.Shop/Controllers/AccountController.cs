using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020105.BusinessLayers;
using SV22T1020105.Models.Partner;

namespace SV22T1020105.Shop.Controllers
{
    public class AccountController : Controller
    {
        #region Login / Logout

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.UserName = username;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập đầy đủ thông tin");
                return View();
            }

            try
            {
                string hashedPwd = CryptHelper.HashMD5(password);
                var account = await SecurityDataService.AuthorizeCustomerAsync(username, hashedPwd);
                if (account == null)
                {
                    ModelState.AddModelError("Error", "Email hoặc mật khẩu không đúng");
                    return View();
                }

                var userData = new CustomerUserData
                {
                    UserId = account.UserId,
                    UserName = account.UserName,
                    DisplayName = account.DisplayName,
                    Email = account.Email
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    userData.CreatePrincipal());

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", $"Đã xảy ra lỗi: {ex.Message}");
                return View();
            }
        }

        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        #endregion

        #region Register

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View(new Customer());
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(Customer data, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(data.CustomerName))
                ModelState.AddModelError(nameof(data.CustomerName), "Vui lòng nhập họ tên");
            if (string.IsNullOrWhiteSpace(data.Email))
                ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email");
            else if (!(await PartnerDataService.ValidateCustomerEmailAsync(data.Email)))
                ModelState.AddModelError(nameof(data.Email), "Email này đã được sử dụng");
            if (string.IsNullOrWhiteSpace(data.Province))
                ModelState.AddModelError(nameof(data.Province), "Vui lòng nhập tỉnh/thành phố");
            if (string.IsNullOrWhiteSpace(password))
                ModelState.AddModelError("password", "Vui lòng nhập mật khẩu");
            else if (password.Length < 6)
                ModelState.AddModelError("password", "Mật khẩu phải ít nhất 6 ký tự");
            if (string.IsNullOrWhiteSpace(confirmPassword))
                ModelState.AddModelError("confirmPassword", "Vui lòng xác nhận mật khẩu");
            else if (password != confirmPassword)
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp");

            if (!ModelState.IsValid)
                return View(data);

            try
            {
                if (string.IsNullOrEmpty(data.ContactName)) data.ContactName = data.CustomerName;
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";
                data.IsLocked = false;

                await PartnerDataService.AddCustomerAsync(data);

                // Đặt mật khẩu cho tài khoản vừa tạo
                await SecurityDataService.ChangeCustomerPasswordAsync(data.Email, CryptHelper.HashMD5(password));

                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(data);
            }
        }

        #endregion

        #region Profile

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userData = User.GetCustomerData();
            if (userData == null) return RedirectToAction("Login");

            int customerId = int.Parse(userData.UserId);
            var model = await PartnerDataService.GetCustomerAsync(customerId);
            if (model == null) return RedirectToAction("Login");

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Profile(Customer data)
        {
            var userData = User.GetCustomerData();
            if (userData == null) return RedirectToAction("Login");

            data.CustomerID = int.Parse(userData.UserId);

            if (string.IsNullOrWhiteSpace(data.CustomerName))
                ModelState.AddModelError(nameof(data.CustomerName), "Vui lòng nhập họ tên");
            if (string.IsNullOrWhiteSpace(data.Province))
                ModelState.AddModelError(nameof(data.Province), "Vui lòng nhập tỉnh/thành phố");
            if (string.IsNullOrWhiteSpace(data.Email))
                ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email");
            else if (!(await PartnerDataService.ValidateCustomerEmailAsync(data.Email, data.CustomerID)))
                ModelState.AddModelError(nameof(data.Email), "Email này đã được sử dụng");

            if (!ModelState.IsValid)
                return View(data);

            try
            {
                if (string.IsNullOrEmpty(data.ContactName)) data.ContactName = data.CustomerName;
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";

                await PartnerDataService.UpdateCustomerAsync(data);
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(data);
            }
        }

        #endregion

        #region Change Password

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            var userData = User.GetCustomerData();
            if (userData == null) return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(oldPassword))
                ModelState.AddModelError("oldPassword", "Vui lòng nhập mật khẩu cũ");
            if (string.IsNullOrWhiteSpace(newPassword))
                ModelState.AddModelError("newPassword", "Vui lòng nhập mật khẩu mới");
            else if (newPassword.Length < 6)
                ModelState.AddModelError("newPassword", "Mật khẩu mới phải ít nhất 6 ký tự");
            if (string.IsNullOrWhiteSpace(confirmPassword))
                ModelState.AddModelError("confirmPassword", "Vui lòng xác nhận mật khẩu mới");
            else if (newPassword != confirmPassword)
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp");

            if (!ModelState.IsValid)
                return View();

            try
            {
                string hashedOld = CryptHelper.HashMD5(oldPassword);
                var check = await SecurityDataService.AuthorizeCustomerAsync(userData.UserName, hashedOld);
                if (check == null)
                {
                    ModelState.AddModelError("oldPassword", "Mật khẩu cũ không đúng");
                    return View();
                }

                await SecurityDataService.ChangeCustomerPasswordAsync(userData.UserName, CryptHelper.HashMD5(newPassword));
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại.";
                HttpContext.Session.Clear();
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View();
            }
        }

        #endregion
    }
}
