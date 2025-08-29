using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class UploadedRow
    {
        public long Id { get; set; }

        [Required]
        public long UploadedSheetId { get; set; }

        [Required]
        public int RowIndex { get; set; }

        [Required]
        public string JsonPayload { get; set; } = string.Empty;

        public string? NormalizedText { get; set; }

        [MaxLength(128)]
        public string? Sku { get; set; }

        [MaxLength(512)]
        public string? Name { get; set; }

        [MaxLength(128)]
        public string? Brand { get; set; }

        [MaxLength(128)]
        public string? Material { get; set; }

        [MaxLength(64)]
        public string? Diameter { get; set; }

        [Required]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public UploadedSheet UploadedSheet { get; set; } = default!;
        public ICollection<UploadedRowMatch> Matches { get; set; } = new List<UploadedRowMatch>();

        public decimal? Price { get; set; }

        [MaxLength(3)]
        public string? Currency { get; set; }

        [MaxLength(32)]
        public string? Position { get; set; }
        [MaxLength(256)]
        public string? MainCategory { get; set; }
        [MaxLength(256)]
        public string? SubCategory { get; set; }
        public string? Description { get; set; }     // nvarchar(max)
        [MaxLength(64)]
        public string? Size { get; set; }
    }
}