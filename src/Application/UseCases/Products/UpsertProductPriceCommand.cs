using Application.Abstractions.Products;

namespace Application.UseCases.Products
{
    public sealed class UpsertProductPriceCommand
    {
        private readonly IProductCatalogReader _catalog;
        public UpsertProductPriceCommand(IProductCatalogReader catalog) => _catalog = catalog;

        public Task ExecuteAsync(string userId, long productId, string currency, decimal price, CancellationToken ct = default)
            => _catalog.UpsertPriceAsync(userId, productId, currency, price, ct);
    }
}