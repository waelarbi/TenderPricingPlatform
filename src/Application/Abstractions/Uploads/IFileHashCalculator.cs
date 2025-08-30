namespace Application.Abstractions.Uploads
{
    public interface IFileHashCalculator
    {
        ValueTask<string> ComputeSha256Async(Stream stream, CancellationToken ct = default);
    }
}
