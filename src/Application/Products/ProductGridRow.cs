namespace Application.Products
{
    public sealed class ProductGridRow
    {
        public long ProductId { get; set; }
        public string Sku { get; set; } = "";
        public string? Name { get; set; }
        public string? Supplier { get; set; }
        public string? Size { get; set; }
        public string? Description { get; set; }

        public decimal? MyPrice { get; set; }
        public string Currency { get; set; } = "EUR";
    }
}