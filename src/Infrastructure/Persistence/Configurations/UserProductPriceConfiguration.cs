using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class UserProductPriceConfiguration : IEntityTypeConfiguration<UserProductPrice>
    {
        public void Configure(EntityTypeBuilder<UserProductPrice> b)
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Currency).HasMaxLength(8).IsRequired();

            b.HasOne(x => x.Product)
             .WithMany() // or .WithMany(p => p.UserPrices) if you add a collection
             .HasForeignKey(x => x.ProductDescriptionId)
             .OnDelete(DeleteBehavior.Cascade);

            // Uniqueness: one price per (User, Product, Currency)
            b.HasIndex(x => new { x.UserId, x.ProductDescriptionId, x.Currency })
             .IsUnique();
        }
    }
}
