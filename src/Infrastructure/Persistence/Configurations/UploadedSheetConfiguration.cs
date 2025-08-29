using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class UploadedSheetConfiguration : IEntityTypeConfiguration<UploadedSheet>
    {
        public void Configure(EntityTypeBuilder<UploadedSheet> b)
        {
            b.HasKey(x => x.Id);

            b.Property(x => x.SheetName).HasMaxLength(128).IsRequired();
            b.Property(x => x.ParseStatus).IsRequired();
            b.Property(x => x.Notes).HasColumnType("nvarchar(max)");

            b.HasOne(x => x.UploadedFile)
                .WithMany(f => f.Sheets)
                .HasForeignKey(x => x.UploadedFileId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.UploadedFileId);
        }
    }
}