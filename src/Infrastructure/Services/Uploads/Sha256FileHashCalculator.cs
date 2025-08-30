using Application.Abstractions.Uploads;
using System.Security.Cryptography;

namespace Infrastructure.Services.Uploads
{
    public sealed class Sha256FileHashCalculator : IFileHashCalculator
    {
        public async ValueTask<string> ComputeSha256Async(Stream stream, CancellationToken ct = default)
        {
            stream.Position = 0;
            using var sha = SHA256.Create();
            var hash = await sha.ComputeHashAsync(stream, ct);
            stream.Position = 0;
            return Convert.ToHexString(hash);
        }
    }
}
