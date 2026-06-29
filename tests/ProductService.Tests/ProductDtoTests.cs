using ProductService.DTOs;
using ProductService.Models;

namespace ProductService.Tests;

public class CreateProductRequestTests
{
    [Fact]
    public void CreateProductRequest_Stores_AllProperties()
    {
        var req = new CreateProductRequest("Widget", "A useful widget", 9.99m, 50, "Tools");

        Assert.Equal("Widget",          req.Name);
        Assert.Equal("A useful widget", req.Description);
        Assert.Equal(9.99m,             req.Price);
        Assert.Equal(50,                req.StockQuantity);
        Assert.Equal("Tools",           req.Category);
    }

    [Fact]
    public void CreateProductRequest_RecordEquality_TwoIdenticalRecordsAreEqual()
    {
        var a = new CreateProductRequest("Gadget", "Desc", 19.99m, 10, "Electronics");
        var b = new CreateProductRequest("Gadget", "Desc", 19.99m, 10, "Electronics");

        Assert.Equal(a, b);
    }

    [Fact]
    public void CreateProductRequest_RecordEquality_DifferentPriceIsNotEqual()
    {
        var a = new CreateProductRequest("Gadget", "Desc", 19.99m, 10, "Electronics");
        var b = new CreateProductRequest("Gadget", "Desc", 29.99m, 10, "Electronics");

        Assert.NotEqual(a, b);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(999.99)]
    [InlineData(9999999.99)]
    public void CreateProductRequest_AcceptsAnyPositivePrice(double price)
    {
        var req = new CreateProductRequest("Item", "Desc", (decimal)price, 1, "General");

        Assert.Equal((decimal)price, req.Price);
    }
}

public class UpdateProductRequestTests
{
    [Fact]
    public void UpdateProductRequest_Stores_AllProperties()
    {
        var req = new UpdateProductRequest("Updated Widget", "New desc", 14.99m, 75, "Tools");

        Assert.Equal("Updated Widget", req.Name);
        Assert.Equal("New desc",       req.Description);
        Assert.Equal(14.99m,           req.Price);
        Assert.Equal(75,               req.StockQuantity);
        Assert.Equal("Tools",          req.Category);
    }
}

public class ProductModelTests
{
    [Fact]
    public void Product_DefaultValues_AreCorrect()
    {
        var product = new Product();

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal(string.Empty,  product.Name);
        Assert.Equal(string.Empty,  product.Description);
        Assert.Equal(0m,            product.Price);
        Assert.Equal(0,             product.StockQuantity);
        Assert.Equal(string.Empty,  product.Category);
    }

    [Fact]
    public void Product_CreatedAt_DefaultsToRecentUtcTime()
    {
        var before  = DateTime.UtcNow.AddSeconds(-1);
        var product = new Product();
        var after   = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(product.CreatedAt, before, after);
    }
}
