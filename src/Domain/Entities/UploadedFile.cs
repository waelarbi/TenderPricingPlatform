using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class UploadedFile
    {
        public long Id { get; set; }

        [Required, MaxLength(260)]
        public string OriginalFileName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ContentType { get; set; }

        public long? ByteSize { get; set; }

        // 32 bytes for SHA-256
        public byte[]? HashSha256 { get; set; }

        [MaxLength(450)]
        public string? UploadedByUserId { get; set; }

        [Required]
        public DateTime UploadedUtc { get; set; } = DateTime.UtcNow;

        [Required]
        public UploadedFileStatus Status { get; set; } = UploadedFileStatus.Queued;

        public string? Notes { get; set; }

        public ICollection<UploadedSheet> Sheets { get; set; } = new List<UploadedSheet>();

        public string ContentHash { get; set; } = string.Empty; // SHA-256 hex, required
        public long FileSizeBytes { get; set; }               // original size
        public string? OriginalFileNameNormalized { get; set; } // name without " (1)"
    }
}