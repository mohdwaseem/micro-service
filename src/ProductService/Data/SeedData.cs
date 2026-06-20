using ProductService.Models;

namespace ProductService.Data;

public static class SeedData
{
    public static async Task SeedAsync(ProductDbContext db)
    {
        if (db.Products.Any()) return;

        var products = new List<Product>
        {
            new() { Name = "Laptop Pro 15", Description = "High-performance laptop with 16GB RAM and 512GB SSD", Price = 1299.99m, StockQuantity = 50, Category = "Electronics" },
            new() { Name = "Wireless Headphones", Description = "Noise-cancelling Bluetooth headphones with 30hr battery", Price = 249.99m, StockQuantity = 100, Category = "Electronics" },
            new() { Name = "Mechanical Keyboard", Description = "Compact TKL mechanical keyboard with Cherry MX switches", Price = 129.99m, StockQuantity = 75, Category = "Peripherals" },
            new() { Name = "USB-C Hub 7-in-1", Description = "USB-C hub with HDMI, USB-A x3, SD card, and PD charging", Price = 59.99m, StockQuantity = 200, Category = "Accessories" },
            new() { Name = "27\" 4K Monitor", Description = "IPS 4K display, 144Hz refresh rate, HDR400 support", Price = 599.99m, StockQuantity = 30, Category = "Monitors" },
        };

        db.Products.AddRange(products);
        await db.SaveChangesAsync();
    }
}
