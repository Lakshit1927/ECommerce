using Microsoft.EntityFrameworkCore;
using ProductService.Entities;

namespace ProductService.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            
            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(p => p.Description)
                .HasMaxLength(500);
                
            entity.Property(p => p.Price)
                .HasPrecision(18, 2);
                
            entity.Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(p => p.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is Product && (
                e.State == EntityState.Added ||
                e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            if (entityEntry.State == EntityState.Added)
            {
                ((Product)entityEntry.Entity).CreatedAt = DateTime.UtcNow;
            }
            
            ((Product)entityEntry.Entity).UpdatedAt = DateTime.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
