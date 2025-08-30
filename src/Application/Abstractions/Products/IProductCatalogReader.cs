using Application.DTOs.Products;

namespace Application.Abstractions.Products
{
    public interface IProductCatalogReader
    {
        Task<(IReadOnlyList<ProductGridRow> Rows, int Total)> GetPagedAsync(
            string userId, string? q, long? supplierId, int page, int pageSize, string currency, CancellationToken ct);

        Task UpsertPriceAsync(string userId, long productId, string currency, decimal price, CancellationToken ct);
    }
}