using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Entities;
using ProductService.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ProductService.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/products")
            .WithTags("Products")
            .WithOpenApi();

        // Public endpoints (no auth required for reading products)
        group.MapGet("/", GetAllProductsAsync)
            .WithName("GetAllProducts")
            .WithSummary("Get all products")
            .Produces<IEnumerable<ProductDto>>(200);

        group.MapGet("/{id:int}", GetProductByIdAsync)
            .WithName("GetProductById")
            .WithSummary("Get product by ID")
            .Produces<ProductDto>(200)
            .Produces(404);

        // Batch endpoint for OrderService
        group.MapGet("/batch", GetProductsByIdsAsync)
            .WithName("GetProductsByIds")
            .WithSummary("Get multiple products by IDs")
            .Produces<IEnumerable<ProductDto>>(200);

        // Protected endpoints (require authentication)
        var protectedGroup = group.RequireAuthorization();

        protectedGroup.MapPost("/", CreateProductAsync)
            .WithName("CreateProduct")
            .WithSummary("Create a new product")
            .Produces<ProductDto>(201)
            .Produces<ValidationProblemDetails>(400);

        protectedGroup.MapPut("/{id:int}", UpdateProductAsync)
            .WithName("UpdateProduct")
            .WithSummary("Update a product")
            .Produces(204)
            .Produces<ValidationProblemDetails>(400)
            .Produces(404);

        protectedGroup.MapDelete("/{id:int}", DeleteProductAsync)
            .WithName("DeleteProduct")
            .WithSummary("Delete a product")
            .Produces(204)
            .Produces(404);
    }

    private static async Task<IResult> GetAllProductsAsync(
        ProductDbContext db,
        ILogger<Program> logger)
    {
        try
        {
            var products = await db.Products
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return Results.Ok(products);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all products");
            return Results.Problem("An error occurred while retrieving products");
        }
    }

    private static async Task<IResult> GetProductByIdAsync(
        int id,
        ProductDbContext db,
        ILogger<Program> logger)
    {
        try
        {
            var product = await db.Products.FindAsync(id);
            if (product == null)
            {
                return Results.NotFound();
            }

            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };

            return Results.Ok(productDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving product with ID: {ProductId}", id);
            return Results.Problem("An error occurred while retrieving the product");
        }
    }

    private static async Task<IResult> GetProductsByIdsAsync(
        [FromQuery] string ids,
        ProductDbContext db,
        ILogger<Program> logger)
    {
        try
        {
            if (string.IsNullOrEmpty(ids))
            {
                return Results.BadRequest("IDs parameter is required");
            }

            var productIds = ids.Split(',')
                .Select(id => int.TryParse(id.Trim(), out var parsed) ? parsed : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

            if (!productIds.Any())
            {
                return Results.BadRequest("No valid IDs provided");
            }

            var products = await db.Products
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return Results.Ok(products);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving products with IDs: {ProductIds}", ids);
            return Results.Problem("An error occurred while retrieving products");
        }
    }

    private static async Task<IResult> CreateProductAsync(
        [FromBody] CreateProductDto dto,
        ProductDbContext db,
        ILogger<Program> logger)
    {
        try
        {
            var product = new Product
            {
                Name = dto.Name.Trim(),
                Description = dto.Description.Trim(),
                Price = dto.Price
            };

            db.Products.Add(product);
            await db.SaveChangesAsync();

            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };

            logger.LogInformation("Product created successfully: {ProductId}", product.Id);
            return Results.Created($"/products/{product.Id}", productDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating product: {ProductName}", dto.Name);
            return Results.Problem("An error occurred while creating the product");
        }
    }

    private static async Task<IResult> UpdateProductAsync(
        int id,
        [FromBody] UpdateProductDto dto,
        ProductDbContext db,
        ILogger<Program> logger)
    {
        try
        {
            var product = await db.Products.FindAsync(id);
            if (product == null)
            {
                return Results.NotFound();
            }

            product.Name = dto.Name.Trim();
            product.Description = dto.Description.Trim();
            product.Price = dto.Price;

            await db.SaveChangesAsync();

            logger.LogInformation("Product updated successfully: {ProductId}", id);
            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating product: {ProductId}", id);
            return Results.Problem("An error occurred while updating the product");
        }
    }

    private static async Task<IResult> DeleteProductAsync(
        int id,
        ProductDbContext db,
        ILogger<Program> logger)
    {
        try
        {
            var product = await db.Products.FindAsync(id);
            if (product == null)
            {
                return Results.NotFound();
            }

            db.Products.Remove(product);
            await db.SaveChangesAsync();

            logger.LogInformation("Product deleted successfully: {ProductId}", id);
            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting product: {ProductId}", id);
            return Results.Problem("An error occurred while deleting the product");
        }
    }
}
