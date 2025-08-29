namespace Domain.Entities
{
    public class UserProductPrice
    {
        public long Id { get; set; }

        public long ProductDescriptionId { get; set; }
        public ProductDescription Product { get; set; } = null!;

        public string UserId { get; set; } = null!;

        public string Currency { get; set; } = "EUR";
        public decimal Price { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedUtc { get; set; }
    }
}