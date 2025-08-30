using Application.DTOs.Uploads;

namespace Application.Abstractions.Uploads
{
    public interface IUploadIngestionService
    {
        Task<UploadPreviewResult> PreviewExcelAsync(Stream fileStream, string fileName, CancellationToken ct);
        Task<int> SaveAsync(UploadPreviewResult preview, string uploadedByUserId, string currency, CancellationToken ct);
    }
}