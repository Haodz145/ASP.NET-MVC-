using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020362.BusinessLayers;
using SV22T1020362.Models.Catalog;
using SV22T1020362.Models.Common;

namespace SV22T1020362.Admin.Controllers
{
    /// <summary>
    /// Các chức năng liên quan đến loại hàng
    /// Chỉ Administrator và DataManager mới có quyền truy cập.
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class CategoryController : Controller
    {
        public const int PAGESIZE = 10;
        public const string SEARCH_CATEGORY = "SearchCategory";

        /// <summary>
        /// Nhập đầu vào tìm kiếm và hiển thị kết quả tìm
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SEARCH_CATEGORY);
            if (input == null)
                input = new PaginationSearchInput() { Page = 1, PageSize = PAGESIZE, SearchValue = "" };
            return View(input);
        }

        /// <summary>
        /// Tìm kiếm và trả về kết quả phân trang
        /// </summary>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await CatalogDataService.ListCategoriesAsync(input);
            ApplicationContext.SetSessionData(SEARCH_CATEGORY, input);
            return View(result);
        }

        /// <summary>
        /// Bổ sung loại hàng mới
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung Loại hàng";
            var model = new Category() { CategoryID = 0 };
            return View("Edit", model);
        }

        /// <summary>
        /// Cập nhật thông tin loại hàng
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin Loại hàng";
            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            return View(model);
        }

        /// <summary>
        /// Lưu dữ liệu loại hàng (bổ sung hoặc cập nhật)
        /// </summary>
        /// <param name="data">Dữ liệu loại hàng từ form</param>
        [HttpPost]
        public async Task<IActionResult> SaveData(Category data)
        {
            ViewBag.Title = data.CategoryID == 0 ? "Bổ sung Loại hàng" : "Cập nhật thông tin Loại hàng";

            if (string.IsNullOrWhiteSpace(data.CategoryName))
                ModelState.AddModelError(nameof(data.CategoryName), "Vui lòng nhập tên loại hàng");

            if (!ModelState.IsValid)
                return View("Edit", data);

            try
            {
                if (data.CategoryID == 0)
                    await CatalogDataService.AddCategoryAsync(data);
                else
                    await CatalogDataService.UpdateCategoryAsync(data);

                ApplicationContext.SetSessionData(SEARCH_CATEGORY, new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = PAGESIZE,
                    SearchValue = data.CategoryName
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
        /// Xóa loại hàng
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await CatalogDataService.DeleteCategoryAsync(id);
                return RedirectToAction("Index");
            }

            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.AllowDelete = !(await CatalogDataService.IsUsedCategoryAsync(id));
            return View(model);
        }
    }
}