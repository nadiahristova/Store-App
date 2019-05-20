using InventoryManager.Model;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.Data
{
    public class InventoryManagerContext: DbContext
    {
        public InventoryManagerContext(DbContextOptions<InventoryManagerContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }

        public DbSet<BookCatalogEntry> BookCatalog { get; set; }

        public DbSet<Author> Authors { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Category>()
            .HasIndex(u => u.Name)
            .IsUnique();

            builder.Entity<Author>()
            .HasIndex(u => u.FullName)
            .IsUnique();

            builder.Entity<BookCatalogEntry>()
            .HasIndex(u => u.BookTitle)
            .IsUnique();

            builder.Entity<BookCatalogEntry>()
            .HasIndex(u => new
                {
                    u.BookTitle,
                    u.AuthorId
                })
            .IsUnique();
        }
    }
}
