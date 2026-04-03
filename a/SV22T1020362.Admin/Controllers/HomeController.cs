using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020362.Admin.Models;
using System.Diagnostics;

namespace SV22T1020362.Admin.Controllers
{
    /// <summary>
    /// Trang chủ
    /// </summary>
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Trang chủ (Dashboard)
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            // BaseController tự động kiểm tra đăng nhập
            // CurrentUser đã có sẵn
            ViewBag.Title = "Trang chủ";
            return View();
        }
    }
}
