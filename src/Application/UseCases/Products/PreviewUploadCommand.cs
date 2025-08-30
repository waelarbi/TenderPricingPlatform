using Application.Abstractions.Uploads;
using Application.DTOs.Uploads;

namespace Application.UseCases.Products
{
    /// <summary>
    /// Parses an Excel file and returns a preview (no persistence).
    /// </summary>
    public sealed class PreviewUploadCommand
    {
        private readonly IUploadIngestionService _ingestion;

        public PreviewUploadCommand(IUploadIngestionService ingestion)
            => _ingestion = ingestion;

        public Task<UploadPreviewResult> ExecuteAsync(
            Stream fileStream, string fileName, CancellationToken ct = default)
            => _ingestion.PreviewExcelAsync(fileStream, fileName, ct);
    }
}
