using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Dtos;
using OrderService.Api.Dtos;
using OrderService.Entities;

namespace OrderService.Endpoints;

public static class OrdersEndpoints
{
    public static void MapOrdersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/orders");

        group.MapGet("/", async (OrderServiceContext db) =>
            await db.Orders.ToListAsync());

       group.MapGet("/{id}", async (int id, OrderServiceContext db, IHttpClientFactory httpClientFactory) =>

{
    var order = await db.Orders.FindAsync(id);
    if (order is null) return Results.NotFound();

    var client = httpClientFactory.CreateClient("ProductService");

    var products = new List<ProductDto>();

    foreach (var productId in order.ProductIds)
    {
        var response = await client.GetAsync($"/products/{productId}");
        if (response.IsSuccessStatusCode)
        {
            var product = await response.Content.ReadFromJsonAsync<ProductDto>();
            if (product is not null)
            {
                products.Add(product);
            }
        }
    }

    var result = new
    {
        order.Id,
        order.CustomerName,
        order.OrderDate,
        Products = products
    };

    return Results.Ok(result);
});
        app.MapPost("/orders", async (
            CreateOrderDto createOrderDto,
            OrderServiceContext dbContext,
            IHttpClientFactory httpClientFactory
        ) =>
        {
            var client = httpClientFactory.CreateClient("ProductService");

            decimal totalAmount = 0;

            foreach (var productId in createOrderDto.ProductIds)
            {
                var response = await client.GetAsync($"/products/{productId}");
                if (!response.IsSuccessStatusCode)
                {
                    return Results.BadRequest($"Product with ID {productId} not found.");
                }

                var product = await response.Content.ReadFromJsonAsync<ProductDto>();
                if (product is null)
                {
                    return Results.BadRequest($"Could not read product info for ID {productId}.");
                }

                totalAmount += product.Price;
            }

            var order = new Order
            {
                CustomerName = createOrderDto.CustomerName,
                Address = createOrderDto.Address,
                ProductIds = createOrderDto.ProductIds,
                TotalAmount = totalAmount,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();

            return Results.Created($"/orders/{order.Id}", order);
        });

        group.MapPut("/{id}", async (int id, UpdateOrderDto dto, OrderServiceContext db) =>
        {
            var order = await db.Orders.FindAsync(id);
            if (order is null) return Results.NotFound();

            order.CustomerName = dto.CustomerName;
            order.Address = dto.Address;
            order.ProductIds = dto.ProductIds;
            order.TotalAmount = dto.TotalAmount;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/{id}", async (int id, OrderServiceContext db) =>
        {
            var order = await db.Orders.FindAsync(id);
            if (order is null) return Results.NotFound();

            db.Orders.Remove(order);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });
    }
}
