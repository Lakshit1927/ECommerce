using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Dtos;
using OrderService.Entities;
using OrderService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OrderService.Endpoints;

public static class OrdersEndpoints
{
    public static void MapOrdersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/orders")
            .RequireAuthorization()
            .WithTags("Orders")
            .WithOpenApi();

        group.MapGet("/", GetAllOrdersAsync)
            .WithName("GetAllOrders")
            .WithSummary("Get all orders")
            .Produces<IEnumerable<OrderDto>>(200);

        group.MapGet("/{id:int}", GetOrderByIdAsync)
            .WithName("GetOrderById")
            .WithSummary("Get order by ID with product details")
            .Produces<object>(200)
            .Produces(404);

        group.MapPost("/", CreateOrderAsync)
            .WithName("CreateOrder")
            .WithSummary("Create a new order")
            .Produces<OrderDto>(201)
            .Produces<ValidationProblemDetails>(400);

        group.MapPut("/{id:int}", UpdateOrderAsync)
            .WithName("UpdateOrder")
            .WithSummary("Update an order")
            .Produces(204)
            .Produces<ValidationProblemDetails>(400)
            .Produces(404);

        group.MapDelete("/{id:int}", DeleteOrderAsync)
            .WithName("DeleteOrder")
            .WithSummary("Delete an order")
            .Produces(204)
            .Produces(404);
    }

    private static async Task<IResult> GetAllOrdersAsync(
        OrderServiceContext db,
        ILogger<Program> logger)
    {
        try
        {
            var orders = await db.Orders
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    CustomerName = o.CustomerName,
                    Address = o.Address,
                    ProductIds = o.ProductIds,
                    TotalAmount = o.TotalAmount,
                    OrderDate = o.OrderDate,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(orders);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all orders");
            return Results.Problem("An error occurred while retrieving orders");
        }
    }

    private static async Task<IResult> GetOrderByIdAsync(
        int id,
        OrderServiceContext db,
        IProductService productService,
        ILogger<Program> logger)
    {
        try
        {
            var order = await db.Orders.FindAsync(id);
            if (order == null)
            {
                return Results.NotFound();
            }

            // Use batch service to get all products at once (fixes N+1 problem)
            var products = await productService.GetProductsAsync(order.ProductIds);

            var result = new
            {
                order.Id,
                order.CustomerName,
                order.Address,
                order.TotalAmount,
                order.OrderDate,
                order.CreatedAt,
                Products = products.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Price
                })
            };

            return Results.Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Products not found"))
        {
            logger.LogWarning("Order {OrderId} references non-existent products: {Message}", id, ex.Message);
            return Results.Problem(ex.Message, statusCode: 422);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving order with ID: {OrderId}", id);
            return Results.Problem("An error occurred while retrieving the order");
        }
    }

    private static async Task<IResult> CreateOrderAsync(
        [FromBody] CreateOrderDto dto,
        OrderServiceContext dbContext,
        IProductService productService,
        ILogger<Program> logger)
    {
        using var transaction = await dbContext.Database.BeginTransactionAsync();
        
        try
        {
            // Validate and calculate total amount using batch service
            var totalAmount = await productService.CalculateTotalPriceAsync(dto.ProductIds);

            var order = new Order
            {
                CustomerName = dto.CustomerName.Trim(),
                Address = dto.Address.Trim(),
                ProductIds = dto.ProductIds.Distinct().ToList(), // Remove duplicates
                TotalAmount = totalAmount,
                OrderDate = dto.OrderDate
            };

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            var orderDto = new OrderDto
            {
                Id = order.Id,
                CustomerName = order.CustomerName,
                Address = order.Address,
                ProductIds = order.ProductIds,
                TotalAmount = order.TotalAmount,
                OrderDate = order.OrderDate,
                CreatedAt = order.CreatedAt
            };

            logger.LogInformation("Order created successfully: {OrderId} for customer: {CustomerName}", 
                order.Id, order.CustomerName);

            return Results.Created($"/orders/{order.Id}", orderDto);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Products not found"))
        {
            await transaction.RollbackAsync();
            logger.LogWarning("Order creation failed due to invalid products: {Message}", ex.Message);
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error creating order for customer: {CustomerName}", dto.CustomerName);
            return Results.Problem("An error occurred while creating the order");
        }
    }

    private static async Task<IResult> UpdateOrderAsync(
        int id,
        [FromBody] UpdateOrderDto dto,
        OrderServiceContext db,
        IProductService productService,
        ILogger<Program> logger)
    {
        using var transaction = await db.Database.BeginTransactionAsync();
        
        try
        {
            var order = await db.Orders.FindAsync(id);
            if (order == null)
            {
                return Results.NotFound();
            }

            // If product IDs changed, validate and recalculate total
            if (!order.ProductIds.SequenceEqual(dto.ProductIds))
            {
                var calculatedTotal = await productService.CalculateTotalPriceAsync(dto.ProductIds);
                
                // Validate that provided total matches calculated total
                if (Math.Abs(dto.TotalAmount - calculatedTotal) > 0.01m)
                {
                    return Results.BadRequest(new 
                    { 
                        error = "Provided total amount doesn't match calculated total",
                        provided = dto.TotalAmount,
                        calculated = calculatedTotal
                    });
                }
                
                order.ProductIds = dto.ProductIds.Distinct().ToList();
                order.TotalAmount = calculatedTotal;
            }
            else
            {
                order.TotalAmount = dto.TotalAmount;
            }

            order.CustomerName = dto.CustomerName.Trim();
            order.Address = dto.Address.Trim();

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogInformation("Order updated successfully: {OrderId}", id);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Products not found"))
        {
            await transaction.RollbackAsync();
            logger.LogWarning("Order update failed due to invalid products: {Message}", ex.Message);
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error updating order: {OrderId}", id);
            return Results.Problem("An error occurred while updating the order");
        }
    }

    private static async Task<IResult> DeleteOrderAsync(
        int id,
        OrderServiceContext db,
        ILogger<Program> logger)
    {
        try
        {
            var order = await db.Orders.FindAsync(id);
            if (order == null)
            {
                return Results.NotFound();
            }

            db.Orders.Remove(order);
            await db.SaveChangesAsync();

            logger.LogInformation("Order deleted successfully: {OrderId}", id);
            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting order: {OrderId}", id);
            return Results.Problem("An error occurred while deleting the order");
        }
    }
}
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Dtos;
using OrderService.Entities;
using OrderService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OrderService.Endpoints;

public static class OrdersEndpoints
{
    public static void MapOrdersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/orders")
            .RequireAuthorization()
            .WithTags("Orders")
            .WithOpenApi();

        group.MapGet("/", GetAllOrdersAsync)
            .WithName("GetAllOrders")
            .WithSummary("Get all orders")
            .Produces<IEnumerable<OrderDto>>(200);

        group.MapGet("/{id:int}", GetOrderByIdAsync)
            .WithName("GetOrderById")
            .WithSummary("Get order by ID with product details")
            .Produces<object>(200)
            .Produces(404);

        group.MapPost("/", CreateOrderAsync)
            .WithName("CreateOrder")
            .WithSummary("Create a new order")
            .Produces<OrderDto>(201)
            .Produces<ValidationProblemDetails>(400);

        group.MapPut("/{id:int}", UpdateOrderAsync)
            .WithName("UpdateOrder")
            .WithSummary("Update an order")
            .Produces(204)
            .Produces<ValidationProblemDetails>(400)
            .Produces(404);

        group.MapDelete("/{id:int}", DeleteOrderAsync)
            .WithName("DeleteOrder")
            .WithSummary("Delete an order")
            .Produces(204)
            .Produces(404);
    }

    private static async Task<IResult> GetAllOrdersAsync(
        OrderServiceContext db,
        ILogger<Program> logger)
    {
        try
        {
            var orders = await db.Orders
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    CustomerName = o.CustomerName,
                    Address = o.Address,
                    ProductIds = o.ProductIds,
                    TotalAmount = o.TotalAmount,
                    OrderDate = o.OrderDate,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(orders);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all orders");
            return Results.Problem("An error occurred while retrieving orders");
        }
    }

    private static async Task<IResult> GetOrderByIdAsync(
        int id,
        OrderServiceContext db,
        IProductService productService,
        ILogger<Program> logger)
    {
        try
        {
            var order = await db.Orders.FindAsync(id);
            if (order == null)
            {
                return Results.NotFound();
            }

            // Use batch service to get all products at once (fixes N+1 problem)
            var products = await productService.GetProductsAsync(order.ProductIds);

            var result = new
            {
                order.Id,
                order.CustomerName,
                order.Address,
                order.TotalAmount,
                order.OrderDate,
                order.CreatedAt,
                Products = products.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Price
                })
            };

            return Results.Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Products not found"))
        {
            logger.LogWarning("Order {OrderId} references non-existent products: {Message}", id, ex.Message);
            return Results.Problem(ex.Message, statusCode: 422);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving order with ID: {OrderId}", id);
            return Results.Problem("An error occurred while retrieving the order");
        }
    }

    private static async Task<IResult> CreateOrderAsync(
        [FromBody] CreateOrderDto dto,
        OrderServiceContext dbContext,
        IProductService productService,
        ILogger<Program> logger)
    {
        using var transaction = await dbContext.Database.BeginTransactionAsync();
        
        try
        {
            // Validate and calculate total amount using batch service
            var totalAmount = await productService.CalculateTotalPriceAsync(dto.ProductIds);

            var order = new Order
            {
                CustomerName = dto.CustomerName.Trim(),
                Address = dto.Address.Trim(),
                ProductIds = dto.ProductIds.Distinct().ToList(), // Remove duplicates
                TotalAmount = totalAmount,
                OrderDate = dto.OrderDate
            };

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            var orderDto = new OrderDto
            {
                Id = order.Id,
                CustomerName = order.CustomerName,
                Address = order.Address,
                ProductIds = order.ProductIds,
                TotalAmount = order.TotalAmount,
                OrderDate = order.OrderDate,
                CreatedAt = order.CreatedAt
            };

            logger.LogInformation("Order created successfully: {OrderId} for customer: {CustomerName}", 
                order.Id, order.CustomerName);

            return Results.Created($"/orders/{order.Id}", orderDto);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Products not found"))
        {
            await transaction.RollbackAsync();
            logger.LogWarning("Order creation failed due to invalid products: {Message}", ex.Message);
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error creating order for customer: {CustomerName}", dto.CustomerName);
            return Results.Problem("An error occurred while creating the order");
        }
    }

    private static async Task<IResult> UpdateOrderAsync(
        int id,
        [FromBody] UpdateOrderDto dto,
        OrderServiceContext db,
        IProductService productService,
        ILogger<Program> logger)
    {
        using var transaction = await db.Database.BeginTransactionAsync();
        
        try
        {
            var order = await db.Orders.FindAsync(id);
            if (order == null)
            {
                return Results.NotFound();
            }

            // If product IDs changed, validate and recalculate total
            if (!order.ProductIds.SequenceEqual(dto.ProductIds))
            {
                var calculatedTotal = await productService.CalculateTotalPriceAsync(dto.ProductIds);
                
                // Validate that provided total matches calculated total
                if (Math.Abs(dto.TotalAmount - calculatedTotal) > 0.01m)
                {
                    return Results.BadRequest(new 
                    { 
                        error = "Provided total amount doesn't match calculated total",
                        provided = dto.TotalAmount,
                        calculated = calculatedTotal
                    });
                }
                
                order.ProductIds = dto.ProductIds.Distinct().ToList();
                order.TotalAmount = calculatedTotal;
            }
            else
            {
                order.TotalAmount = dto.TotalAmount;
            }

            order.CustomerName = dto.CustomerName.Trim();
            order.Address = dto.Address.Trim();

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogInformation("Order updated successfully: {OrderId}", id);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Products not found"))
        {
            await transaction.RollbackAsync();
            logger.LogWarning("Order update failed due to invalid products: {Message}", ex.Message);
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error updating order: {OrderId}", id);
            return Results.Problem("An error occurred while updating the order");
        }
    }

    private static async Task<IResult> DeleteOrderAsync(
        int id,
        OrderServiceContext db,
        ILogger<Program> logger)
    {
        try
        {
            var order = await db.Orders.FindAsync(id);
            if (order == null)
            {
                return Results.NotFound();
            }

            db.Orders.Remove(order);
            await db.SaveChangesAsync();

            logger.LogInformation("Order deleted successfully: {OrderId}", id);
            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting order: {OrderId}", id);
            return Results.Problem("An error occurred while deleting the order");
        }
    }
}
