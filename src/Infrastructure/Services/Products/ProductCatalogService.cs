using Application.Products;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Products
{
    public class ProductCatalogService : IProductCatalogService
    {
        private readonly TenderPriceDbContext _db;
        public ProductCatalogService(TenderPriceDbContext db) => _db = db;

        public async Task<(IReadOnlyList<ProductGridRow>, int)> GetPagedAsync(
            string userId, string? q, long? supplierId, int page, int pageSize, string currency, CancellationToken ct)
        {
            var products = _db.ProductDescriptions.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                products = products.Where(p =>
                    (p.Sku != null && p.Sku.Contains(q)) ||
                    (p.Name != null && p.Name.Contains(q)) ||
                    (p.SearchText != null && p.SearchText.Contains(q)));
            }

            if (supplierId is not null)
                products = products.Where(p => p.SupplierId == supplierId);

            var total = await products.CountAsync(ct);

            var pageRows = await products
                .OrderBy(p => p.Sku) // tweak as desired
                .Skip(page * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Sku,
                    p.Name,
                    Supplier = p.Supplier != null ? p.Supplier.Name : null,
                    p.Diameter,
                    p.Material,
                    p.Category,
                    p.SearchText // optional
                })
                .ToListAsync(ct);

            var ids = pageRows.Select(x => x.Id).ToList();
            
            var myPrices = await _db.UserProductPrices
                .Where(x => x.UserId == userId && x.Currency == currency && ids.Contains(x.ProductDescriptionId))
                .ToListAsync(ct);
            var priceMap = myPrices.ToDictionary(x => x.ProductDescriptionId, x => x.Price);

            var rows = pageRows.Select(x => new ProductGridRow
            {
                ProductId = x.Id,
                Sku = x.Sku ?? "",
                Name = x.Name,
                Supplier = x.Supplier,
                // You can surface Size from wherever you store it (if in ProductDescription or attributes)
                Description = x.SearchText,
                MyPrice = priceMap.TryGetValue(x.Id, out var p) ? p : null,
                Currency = currency
            }).ToList();

            return (rows, total);
        }

        public async Task UpsertPriceAsync(string userId, long productId, string currency, decimal price, CancellationToken ct)
        {
            var existing = await _db.UserProductPrices
                .FirstOrDefaultAsync(x => x.UserId == userId && x.ProductDescriptionId == productId && x.Currency == currency, ct);

            if (existing is null)
            {
                _db.UserProductPrices.Add(new UserProductPrice
                {
                    UserId = userId,
                    ProductDescriptionId = productId,
                    Currency = currency,
                    Price = price,
                    CreatedUtc = DateTime.UtcNow
                });
            }
            else
            {
                existing.Price = price;
                existing.UpdatedUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
