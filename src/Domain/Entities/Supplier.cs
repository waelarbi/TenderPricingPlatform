namespace Domain.Entities
{
    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Country { get; set; }
        public string? Notes { get; set; }

        public ICollection<ProductDescription> Products { get; set; } = new List<ProductDescription>();
    }
}