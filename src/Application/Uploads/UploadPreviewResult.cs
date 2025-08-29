namespace Application.Uploads
{
    public class UploadPreviewResult
    {
        public string FileName { get; set; } = string.Empty;
        public string SheetName { get; set; } = "Sheet1";
        public List<UploadPreviewRow> Rows { get; set; } = new();

        // NEW ↓↓↓
        public string? ContentHash { get; set; }
        public long SizeBytes { get; set; }
        public bool IsDuplicate { get; set; }
        public long? DuplicateFileId { get; set; }
        public string? DuplicateFileName { get; set; }
    }
}
