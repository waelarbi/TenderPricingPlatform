using Application.Abstractions.Products;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Products
{
    public sealed class EfSupplierReader : ISupplierReader
    {
        private readonly TenderPriceDbContext _db;
        public EfSupplierReader(TenderPriceDbContext db) => _db = db;

        public async Task<IReadOnlyList<SupplierListItem>> GetAllAsync(CancellationToken ct = default)
            => await _db.Suppliers
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .Select(s => new SupplierListItem(s.Id, s.Name))
                .ToListAsync(ct);
    }
}