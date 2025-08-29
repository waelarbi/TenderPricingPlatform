namespace Application.Uploads
{
    public class UploadPreviewRow
    {
        public int RowIndex { get; set; }

        public string? Position { get; set; }
        public string? MainCategory { get; set; }
        public string? SubCategory { get; set; }

        public string? Sku { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }   // NEW: full text we’ll search on
        public string? Size { get; set; }          // NEW: replaces Diameter

        public string? Brand { get; set; }         // keep for later (not extracted now)
        public string? Material { get; set; }      // ignored for now

        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public Dictionary<string, string?> Raw { get; set; } = new();
    }
}