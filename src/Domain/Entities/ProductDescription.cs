using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class ProductDescription
    {
        public long Id { get; set; }
        public string? Sku { get; set; }
        public string? Name { get; set; }
        public string? Brand { get; set; }
        public string? Category { get; set; }
        public string? Material { get; set; }
        public string? Diameter { get; set; }
        public string? AttributesJson { get; set; }
        public string? SearchText { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedUtc { get; set; }

        // NEW (provenance)
        public long? SourceFileId { get; set; }
        public long? SourceRowId { get; set; }

        // Optional supplier FK
        public int? SupplierId { get; set; }

        // Navs
        public UploadedFile? SourceFile { get; set; }
        public UploadedRow? SourceRow { get; set; }
        public Supplier? Supplier { get; set; }

        public ICollection<UploadedRowMatch> Matches { get; set; } = new List<UploadedRowMatch>();
    }
}
