using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.DTOs;
using ProductService.Models;

namespace ProductService.Endpoints;

public static class ProductEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        group.MapGet("/", GetAll)
            .WithName("GetAllProducts")
            .WithSummary("List all products")
            .AllowAnonymous();

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetProductById")
            .WithSummary("Get a single product by ID")
            .AllowAnonymous();

        group.MapPost("/", Create)
            .WithName("CreateProduct")
            .WithSummary("Create a new product")
            .RequireAuthorization();

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateProduct")
            .WithSummary("Update an existing product")
            .RequireAuthorization();

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteProduct")
            .WithSummary("Delete a product")
            .RequireAuthorization();
    }

    private static async Task<IResult> GetAll(ProductDbContext db)
    {
        var products = await db.Products.OrderBy(p => p.Name).ToListAsync();
        return Results.Ok(products);
    }

    private static async Task<IResult> GetById(Guid id, ProductDbContext db)
    {
        var product = await db.Products.FindAsync(id);
        return product is null ? Results.NotFound() : Results.Ok(product);
    }

    private static async Task<IResult> Create(CreateProductRequest req, ProductDbContext db)
    {
        var product = new Product
        {
            Name = req.Name,
            Description = req.Description,
            Price = req.Price,
            StockQuantity = req.StockQuantity,
            Category = req.Category
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();
        return Results.Created($"/api/products/{product.Id}", product);
    }

    private static async Task<IResult> Update(Guid id, UpdateProductRequest req, ProductDbContext db)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return Results.NotFound();

        product.Name = req.Name;
        product.Description = req.Description;
        product.Price = req.Price;
        product.StockQuantity = req.StockQuantity;
        product.Category = req.Category;

        await db.SaveChangesAsync();
        return Results.Ok(product);
    }

    private static async Task<IResult> Delete(Guid id, ProductDbContext db)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return Results.NotFound();

        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}
