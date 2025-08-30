using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Products
{
    public interface ISupplierReader
    {
        Task<IReadOnlyList<SupplierListItem>> GetAllAsync(CancellationToken ct = default);
    }

    public sealed record SupplierListItem(long Id, string Name);
}