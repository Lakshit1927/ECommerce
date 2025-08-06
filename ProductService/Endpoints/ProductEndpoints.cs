using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Entities;

namespace ProductService.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/products")
                       .RequireAuthorization(); // 👈 JWT Authorization required

        // GET all products
        group.MapGet("/", async (ProductDbContext db) =>
            await db.Products.ToListAsync());

        // GET product by ID
        group.MapGet("/{id}", async (int id, ProductDbContext db) =>
        {
            var product = await db.Products.FindAsync(id);
            return product is null ? Results.NotFound() : Results.Ok(product);
        });

        // POST - Create new product
        group.MapPost("/", async (Product product, ProductDbContext db) =>
        {
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return Results.Created($"/products/{product.Id}", product);
        });

        // PUT - Update product
        group.MapPut("/{id}", async (int id, Product updatedProduct, ProductDbContext db) =>
        {
            var existing = await db.Products.FindAsync(id);
            if (existing is null) return Results.NotFound();

            existing.Name = updatedProduct.Name;
            existing.Description = updatedProduct.Description;
            existing.Price = updatedProduct.Price;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // DELETE - Remove product
        group.MapDelete("/{id}", async (int id, ProductDbContext db) =>
        {
            var product = await db.Products.FindAsync(id);
            if (product is null) return Results.NotFound();

            db.Products.Remove(product);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
