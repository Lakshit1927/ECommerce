using Microsoft.EntityFrameworkCore;
using Productservice.Data;
using Productservice.Entities;
using ProductService.Dtos;

namespace ProductService.Endpoints;

    public static class ProductEndpoints
    {
        public static RouteGroupBuilder MapProductEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/products");

            // Get All Products
            group.MapGet("/", async (ProductDbContext db) =>
                await db.Products.ToListAsync());

            // Create Product
        group.MapPost("/", async (CreateProductDto dto, ProductDbContext db) =>
    {
        var product = new Product
        {
            Name = dto.Name,
            Genre = dto.Genre,
            Price = dto.Price,
            ReleaseDate = dto.ReleaseDate,
            Description = dto.Description // ✅ Add this
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        return Results.Created($"/products/{product.Id}", product);
    });


            // Get Product by ID
            group.MapGet("/{id}", async (int id, ProductDbContext db) =>
            {
                var product = await db.Products.FindAsync(id);
                return product is not null ? Results.Ok(product) : Results.NotFound();
            });

            // Update Product
            group.MapPut("/{id}", async (int id, UpdateProductDto dto, ProductDbContext db) =>
            {
                var product = await db.Products.FindAsync(id);
                if (product is null) return Results.NotFound();

                product.Name = dto.Name;
                product.Description = dto.Description;
                product.Price = dto.Price;

                await db.SaveChangesAsync();
                return Results.NoContent();
            });

            // Delete Product
            group.MapDelete("/{id}", async (int id, ProductDbContext db) =>
            {
                var product = await db.Products.FindAsync(id);
                if (product is null) return Results.NotFound();

                db.Products.Remove(product);
                await db.SaveChangesAsync();
                return Results.NoContent();
            });

            return group;
        }
    }
