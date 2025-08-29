using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class UploadedRowConfiguration : IEntityTypeConfiguration<UploadedRow>
    {
        public void Configure(EntityTypeBuilder<UploadedRow> b)
        {
            b.HasKey(x => x.Id);

            b.Property(x => x.RowIndex).IsRequired();
            b.Property(x => x.JsonPayload).HasColumnType("nvarchar(max)").IsRequired();
            b.Property(x => x.NormalizedText).HasColumnType("nvarchar(max)");

            b.Property(x => x.Sku).HasMaxLength(128);
            b.Property(x => x.Name).HasMaxLength(512);
            b.Property(x => x.Brand).HasMaxLength(128);
            b.Property(x => x.Material).HasMaxLength(128);
            b.Property(x => x.Price).HasPrecision(18, 4);
            b.Property(x => x.Currency).HasMaxLength(3);
            b.Property(x => x.Position).HasMaxLength(32);
            b.Property(x => x.MainCategory).HasMaxLength(256);
            b.Property(x => x.SubCategory).HasMaxLength(256);
            b.Property(x => x.Description).HasColumnType("nvarchar(max)");
            b.Property(x => x.Size).HasMaxLength(64);

            b.Property(x => x.CreatedUtc).IsRequired();

            b.HasOne(x => x.UploadedSheet)
                .WithMany(s => s.Rows)
                .HasForeignKey(x => x.UploadedSheetId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique per sheet row
            b.HasIndex(x => new { x.UploadedSheetId, x.RowIndex }).IsUnique();

            // Speed SKU lookups
            b.HasIndex(x => x.Sku);
        }
    }
}