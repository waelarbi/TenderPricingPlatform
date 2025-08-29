using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public class TenderPriceDbContext : DbContext
    {
        public DbSet<ProductDescription> ProductDescriptions => Set<ProductDescription>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<UploadedFile> UploadedFiles => Set<UploadedFile>();
        public DbSet<UploadedSheet> UploadedSheets => Set<UploadedSheet>();
        public DbSet<UploadedRow> UploadedRows => Set<UploadedRow>();
        public DbSet<UploadedRowMatch> UploadedRowMatches => Set<UploadedRowMatch>();
        public DbSet<UserProductPrice> UserProductPrices => Set<UserProductPrice>();

        public TenderPriceDbContext(DbContextOptions<TenderPriceDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenderPriceDbContext).Assembly);
        }
    }
}
