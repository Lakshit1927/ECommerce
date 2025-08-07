using Microsoft.EntityFrameworkCore;
using OrderService.Entities;
using System.Text.Json;

namespace OrderService.Data;

public class OrderServiceContext : DbContext
{
    public OrderServiceContext(DbContextOptions<OrderServiceContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            
            entity.Property(o => o.CustomerName)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(o => o.Address)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(o => o.TotalAmount)
                .HasPrecision(18, 2);
                
            entity.Property(o => o.ProductIds)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions?)null) ?? new List<int>()
                )
                .HasColumnType("nvarchar(max)");
                
            entity.Property(o => o.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(o => o.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is Order && (
                e.State == EntityState.Added ||
                e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            if (entityEntry.State == EntityState.Added)
            {
                ((Order)entityEntry.Entity).CreatedAt = DateTime.UtcNow;
            }
            
            ((Order)entityEntry.Entity).UpdatedAt = DateTime.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
