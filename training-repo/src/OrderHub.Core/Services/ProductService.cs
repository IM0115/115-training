using OrderHub.Core.Common;
using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;

namespace OrderHub.Core.Services;

public class ProductService : IProductService
{
    private const int LowStockRecentDays = 30;

    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public Task<IReadOnlyList<Product>> GetAllAsync() => _productRepository.GetAllAsync();

    public Task<IReadOnlyList<Product>> GetActiveAsync() => _productRepository.GetActiveAsync();

    public async Task<ServiceResult<IReadOnlyList<LowStockItem>>> GetLowStockAsync(int threshold)
    {
        if (threshold <= 0)
            return ServiceResult<IReadOnlyList<LowStockItem>>.Fail("門檻必須大於 0");

        var products = await _productRepository.GetLowStockAsync(threshold);
        var since = DateTime.UtcNow.AddDays(-LowStockRecentDays);
        var soldByProduct = await _productRepository.GetSoldQuantitySinceAsync(since);

        var items = products
            .Select(p => new LowStockItem(
                p,
                soldByProduct.TryGetValue(p.Id, out var sold) ? sold : 0))
            .ToList();

        return ServiceResult<IReadOnlyList<LowStockItem>>.Ok(items);
    }
}
