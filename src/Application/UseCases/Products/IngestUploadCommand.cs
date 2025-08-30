using Application.Abstractions.Uploads;
using Application.DTOs.Uploads;

namespace Application.UseCases.Products
{
    /// <summary>
    /// Persists a previously previewed upload (sheets + rows).
    /// </summary>
    public sealed class IngestUploadCommand
    {
        private readonly IUploadIngestionService _ingestion;

        public IngestUploadCommand(IUploadIngestionService ingestion)
            => _ingestion = ingestion;

        /// <returns>UploadedFile Id</returns>
        public Task<int> ExecuteAsync(
            UploadPreviewResult preview, string uploadedByUserId, string currency, CancellationToken ct = default)
            => _ingestion.SaveAsync(preview, uploadedByUserId, currency, ct);
    }
}