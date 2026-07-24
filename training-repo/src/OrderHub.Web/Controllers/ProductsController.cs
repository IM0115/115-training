using Microsoft.AspNetCore.Mvc;
using OrderHub.Core.Services;
using OrderHub.Web.ViewModels;

namespace OrderHub.Web.Controllers;

public class ProductsController : Controller
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _productService.GetAllAsync();

        var vm = new ProductListViewModel
        {
            Products = products.Select(p => new ProductRowViewModel
            {
                Sku = p.Sku,
                Name = p.Name,
                UnitPrice = p.UnitPrice,
                StockQuantity = p.StockQuantity,
                IsActive = p.IsActive
            }).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> LowStock(int threshold = 10)
    {
        var vm = new LowStockViewModel { Threshold = threshold };

        var result = await _productService.GetLowStockAsync(threshold);
        if (!result.Success)
        {
            ModelState.AddModelError(nameof(LowStockViewModel.Threshold), result.ErrorMessage);
            return View(vm);
        }

        vm.Items = result.Value!.Select(x => new LowStockRowViewModel
        {
            Sku = x.Product.Sku,
            Name = x.Product.Name,
            StockQuantity = x.Product.StockQuantity,
            SoldLast30Days = x.SoldLast30Days
        }).ToList();

        return View(vm);
    }
}

