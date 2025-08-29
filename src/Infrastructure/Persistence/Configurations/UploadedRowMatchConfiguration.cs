using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class UploadedRowMatchConfiguration : IEntityTypeConfiguration<UploadedRowMatch>
    {
        public void Configure(EntityTypeBuilder<UploadedRowMatch> b)
        {
            b.HasKey(x => x.Id);

            b.Property(x => x.Score)
                .HasPrecision(5, 4) // 0.0000 to 9.9999
                .IsRequired();

            b.Property(x => x.MatchDetails).HasColumnType("nvarchar(max)");
            b.Property(x => x.CreatedUtc).IsRequired();

            b.HasOne(x => x.UploadedRow)
                .WithMany(r => r.Matches)
                .HasForeignKey(x => x.UploadedRowId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.ProductDescription)
                .WithMany(p => p.Matches)
                .HasForeignKey(x => x.ProductDescriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure single edge per pair (remove if you want multiple runs stored)
            b.HasIndex(x => new { x.UploadedRowId, x.ProductDescriptionId }).IsUnique();

            b.HasIndex(x => x.UploadedRowId);
            b.HasIndex(x => x.ProductDescriptionId);
        }
    }
}