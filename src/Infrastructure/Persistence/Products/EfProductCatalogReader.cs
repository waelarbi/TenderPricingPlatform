using Application.Abstractions.Products;
using Application.DTOs.Products;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Products
{
    public class EfProductCatalogReader : IProductCatalogReader
    {
        private readonly TenderPriceDbContext _db;
        public EfProductCatalogReader(TenderPriceDbContext db) => _db = db;

        public async Task<(IReadOnlyList<ProductGridRow>, int)> GetPagedAsync(
            string userId, string? q, long? supplierId, int page, int pageSize, string currency, CancellationToken ct)
        {
            // Normalize inputs defensively
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;
            currency = (currency ?? string.Empty).Trim();

            // Base query
            var products = _db.ProductDescriptions
                              .AsNoTracking()
                              .AsQueryable();

            // Text search
            //if (!string.IsNullOrWhiteSpace(q))
            //{
            //    q = q.Trim();
            //    products = products.Where(p =>
            //        (p.Sku != null && p.Sku.Contains(q)) ||
            //        (p.Name != null && p.Name.Contains(q)) ||
            //        (p.SearchText != null && p.SearchText.Contains(q)));
            //}

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();

                // If user wrapped query in quotes => exact phrase
                bool exact = q.Length >= 2 && q.StartsWith('"') && q.EndsWith('"');
                if (exact)
                {
                    var phrase = q.Trim('"');
                    products = products.Where(p =>
                        (p.Sku != null && p.Sku.Contains(phrase)) ||
                        (p.Name != null && p.Name.Contains(phrase)) ||
                        (p.SearchText != null && p.SearchText.Contains(phrase)));
                }
                else
                {
                    // Tokenize (keep top few meaningful words)
                    var tokens = Tokenize(q)
                        .Where(t => t.Length >= 2)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(6)
                        .ToArray();

                    foreach (var t in tokens)
                    {
                        var term = t; // closure-safe
                        products = products.Where(p =>
                            (p.Sku != null && p.Sku.Contains(term)) ||
                            (p.Name != null && p.Name.Contains(term)) ||
                            (p.SearchText != null && p.SearchText.Contains(term)));
                    }
                }
            }

            // Supplier filter
            if (supplierId is not null)
                products = products.Where(p => p.SupplierId == supplierId);

            // Count BEFORE paging
            var total = await products.CountAsync(ct);

            // Page slice
            var skip = (page - 1) * pageSize;

            var pageRows = await products
                .OrderBy(p => p.Sku) // adjust if you need deterministic secondary sort
                .Skip(skip)
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
                    p.SearchText
                })
                .ToListAsync(ct);

            var ids = pageRows.Select(x => x.Id).ToList();

            // User prices for the current page (same currency)
            var myPrices = await _db.UserProductPrices.AsNoTracking()
                .Where(x => x.UserId == userId &&
                            x.Currency == currency &&
                            ids.Contains(x.ProductDescriptionId))
                .ToListAsync(ct);

            var priceMap = myPrices.ToDictionary(x => x.ProductDescriptionId, x => (decimal?)x.Price);

            var rows = pageRows.Select(x => new ProductGridRow
            {
                ProductId = x.Id,
                Sku = x.Sku ?? string.Empty,
                Name = x.Name,
                Supplier = x.Supplier,
                Description = x.SearchText, // you said you want description/search text visible
                MyPrice = priceMap.TryGetValue(x.Id, out var p) ? p : null,
                Currency = currency
            }).ToList();

            return (rows, total);
        }

        public async Task UpsertPriceAsync(string userId, long productId, string currency, decimal price, CancellationToken ct)
        {
            currency = (currency ?? string.Empty).Trim();

            var existing = await _db.UserProductPrices
                .FirstOrDefaultAsync(x =>
                        x.UserId == userId &&
                        x.ProductDescriptionId == productId &&
                        x.Currency == currency, ct);

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

        private static IEnumerable<string> Tokenize(string input)
        {
            // split on whitespace/punctuation; normalize multiple spaces
            var sb = new System.Text.StringBuilder(input.Length);
            foreach (var ch in input)
                sb.Append(char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : ' ');

            return sb.ToString()
                     .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
}