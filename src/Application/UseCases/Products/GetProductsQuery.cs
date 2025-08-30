using Application.Abstractions.Products;
using Application.DTOs.Products;

namespace Application.UseCases.Products
{
    /// <summary>
    /// Read-only use case for paging/searching products.
    /// </summary>
    public sealed class GetProductsQuery
    {
        private readonly IProductCatalogReader _reader;

        public GetProductsQuery(IProductCatalogReader reader)
            => _reader = reader;

        public Task<(IReadOnlyList<ProductGridRow> Rows, int Total)> ExecuteAsync(
            string userId, string? q, long? supplierId, int page, int pageSize, string currency, CancellationToken ct = default)
            => _reader.GetPagedAsync(userId, q, supplierId, page, pageSize, currency, ct);
    }
}