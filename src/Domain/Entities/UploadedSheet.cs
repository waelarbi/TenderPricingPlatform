using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class UploadedSheet
    {
        public long Id { get; set; }

        [Required]
        public long UploadedFileId { get; set; }

        [Required, MaxLength(128)]
        public string SheetName { get; set; } = string.Empty;

        public int? RowCount { get; set; }

        [Required]
        public ParseStatus ParseStatus { get; set; } = ParseStatus.Queued;

        public string? Notes { get; set; }

        public UploadedFile UploadedFile { get; set; } = default!;
        public ICollection<UploadedRow> Rows { get; set; } = new List<UploadedRow>();
    }
}