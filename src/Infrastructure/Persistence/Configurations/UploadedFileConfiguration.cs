using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class UploadedFileConfiguration : IEntityTypeConfiguration<UploadedFile>
    {
        public void Configure(EntityTypeBuilder<UploadedFile> b)
        {
            b.HasKey(x => x.Id);

            b.Property(x => x.OriginalFileName).HasMaxLength(260).IsRequired();
            b.Property(x => x.ContentType).HasMaxLength(100);
            b.Property(x => x.UploadedByUserId).HasMaxLength(450);
            b.Property(x => x.HashSha256).HasColumnType("varbinary(32)");
            b.Property(x => x.Status).IsRequired();
            b.Property(x => x.Notes).HasColumnType("nvarchar(max)");
            b.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
            b.Property(x => x.FileSizeBytes).IsRequired();
            b.Property(x => x.OriginalFileNameNormalized).HasMaxLength(260);

            // Optional unique index for dedupe by hash
            b.HasIndex(x => x.HashSha256).IsUnique().HasFilter("[HashSha256] IS NOT NULL");
            b.HasIndex(x => x.ContentHash).IsUnique().HasFilter("[ContentHash] IS NOT NULL AND [ContentHash] <> ''"); // hard guard in SQL
        }
    }
}