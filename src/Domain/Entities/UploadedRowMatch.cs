using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class UploadedRowMatch
    {
        public long Id { get; set; }

        [Required]
        public long UploadedRowId { get; set; }

        [Required]
        public long ProductDescriptionId { get; set; }

        [Required]
        public decimal Score { get; set; } // set precision via Fluent API

        public string? MatchDetails { get; set; }

        [Required]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public UploadedRow UploadedRow { get; set; } = default!;
        public ProductDescription ProductDescription { get; set; } = default!;
    }
}