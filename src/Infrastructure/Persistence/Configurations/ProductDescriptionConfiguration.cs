using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class ProductDescriptionConfiguration : IEntityTypeConfiguration<ProductDescription>
    {
        public void Configure(EntityTypeBuilder<ProductDescription> b)
        {
            b.HasKey(x => x.Id);

            b.Property(x => x.Sku).HasMaxLength(128);
            b.Property(x => x.Name).HasMaxLength(512);
            b.Property(x => x.Brand).HasMaxLength(128);
            b.Property(x => x.Category).HasMaxLength(128);
            b.Property(x => x.Material).HasMaxLength(128);
            b.Property(x => x.Diameter).HasMaxLength(64);
            b.Property(x => x.CreatedUtc).IsRequired();
            b.Property(x => x.SearchText).HasColumnType("nvarchar(max)");
            b.Property(x => x.AttributesJson).HasColumnType("nvarchar(max)");

            b.HasIndex(x => x.Sku).IsUnique(false);

            // provenance FKs (no cascade from master to raw)
            b.HasOne(x => x.SourceFile)
                .WithMany()
                .HasForeignKey(x => x.SourceFileId)
                .OnDelete(DeleteBehavior.NoAction);

            b.HasOne(x => x.SourceRow)
                .WithMany()
                .HasForeignKey(x => x.SourceRowId)
                .OnDelete(DeleteBehavior.NoAction);

            // optional supplier
            b.HasOne(x => x.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}