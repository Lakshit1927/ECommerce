using Microsoft.EntityFrameworkCore;
using OrderService.Entities;
using System.Text.Json;

namespace OrderService.Data;

public class OrderServiceContext(DbContextOptions<OrderServiceContext> options)
    : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .Property(o => o.ProductIds)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions)null)!
            );
    }
}
