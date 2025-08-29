namespace Application.Uploads
{
    public class UploadPreviewResult
    {
        public string FileName { get; set; } = string.Empty;
        public string SheetName { get; set; } = "Sheet1";
        public List<UploadPreviewRow> Rows { get; set; } = new();
    }
}
