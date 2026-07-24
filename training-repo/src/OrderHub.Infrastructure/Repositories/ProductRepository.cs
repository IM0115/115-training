using Microsoft.EntityFrameworkCore;
using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;
using OrderHub.Infrastructure.Data;

namespace OrderHub.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly OrderHubDbContext _db;

    public ProductRepository(OrderHubDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync() =>
        await _db.Products.OrderBy(p => p.Sku).ToListAsync();

    public async Task<IReadOnlyList<Product>> GetActiveAsync() =>
        await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Sku).ToListAsync();

    // 低於門檻且仍在販售的商品，依庫存量升冪（門檻用 < 而非 <=，剛好等於門檻不算低庫存）。
    public async Task<IReadOnlyList<Product>> GetLowStockAsync(int threshold) =>
        await _db.Products
            .Where(p => p.IsActive && p.StockQuantity < threshold)
            .OrderBy(p => p.StockQuantity)
            .ToListAsync();

    // 一次算出各商品自 since 起的售出數量（排除已取消訂單），回傳 ProductId -> 售出量，避免逐商品查詢造成 N+1。
    public async Task<IReadOnlyDictionary<int, int>> GetSoldQuantitySinceAsync(DateTime since)
    {
        var grouped = await _db.OrderItems
            .Where(i => i.Order!.CreatedAt >= since && i.Order.Status != OrderStatus.Cancelled)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Sold = g.Sum(i => i.Quantity) })
            .ToListAsync();

        return grouped.ToDictionary(x => x.ProductId, x => x.Sold);
    }

    public Task<Product?> GetByIdAsync(int id) =>
        _db.Products.FirstOrDefaultAsync(p => p.Id == id);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
