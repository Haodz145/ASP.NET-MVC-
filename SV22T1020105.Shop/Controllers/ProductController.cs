using Microsoft.AspNetCore.Mvc;
using SV22T1020105.BusinessLayers;
using SV22T1020105.Models.Catalog;
using SV22T1020105.Models.Common;

namespace SV22T1020105.Shop.Controllers
{
    public class ProductController : Controller
    {
        private const string SEARCH_PRODUCT = "SearchProductShop";

        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(SEARCH_PRODUCT);
            if (input == null)
                input = new ProductSearchInput
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = "",
                    CategoryID = 0,
                    SupplierID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                };
            return View(input);
        }

        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(SEARCH_PRODUCT, input);
            return PartialView("_ProductSearchResult", result);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var model = await CatalogDataService.GetProductAsync(id);
            if (model == null || !model.IsSelling)
                return RedirectToAction("Index");

            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);
            return View(model);
        }
    }
}
