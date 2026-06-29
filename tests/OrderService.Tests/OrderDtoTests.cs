using OrderService.DTOs;
using OrderService.Models;

namespace OrderService.Tests;

public class OrderLineRequestTests
{
    [Fact]
    public void OrderLineRequest_Stores_AllProperties()
    {
        var productId = Guid.NewGuid();
        var line      = new OrderLineRequest(productId, "Keyboard", 2, 79.99m);

        Assert.Equal(productId,  line.ProductId);
        Assert.Equal("Keyboard", line.ProductName);
        Assert.Equal(2,          line.Quantity);
        Assert.Equal(79.99m,     line.UnitPrice);
    }

    [Fact]
    public void OrderLineRequest_LineTotal_IsQuantityTimesUnitPrice()
    {
        var line = new OrderLineRequest(Guid.NewGuid(), "Mouse", 3, 25.00m);

        var lineTotal = line.Quantity * line.UnitPrice;

        Assert.Equal(75.00m, lineTotal);
    }

    [Fact]
    public void OrderLineRequest_RecordEquality_TwoIdenticalLinesAreEqual()
    {
        var id = Guid.NewGuid();
        var a  = new OrderLineRequest(id, "Monitor", 1, 299.99m);
        var b  = new OrderLineRequest(id, "Monitor", 1, 299.99m);

        Assert.Equal(a, b);
    }
}

public class CreateOrderRequestTests
{
    [Fact]
    public void CreateOrderRequest_WithItems_StoresItems()
    {
        var items = new List<OrderLineRequest>
        {
            new(Guid.NewGuid(), "Keyboard", 1, 49.99m),
            new(Guid.NewGuid(), "Mouse",    1, 29.99m),
        };

        var req = new CreateOrderRequest(items);

        Assert.Equal(2, req.Items.Count);
    }

    [Fact]
    public void CreateOrderRequest_TotalAmount_IsSumOfLineTotals()
    {
        var items = new List<OrderLineRequest>
        {
            new(Guid.NewGuid(), "Keyboard", 2, 49.99m),  // 99.98
            new(Guid.NewGuid(), "Mouse",    3, 29.99m),  // 89.97
        };

        var req   = new CreateOrderRequest(items);
        var total = req.Items.Sum(i => i.Quantity * i.UnitPrice);

        Assert.Equal(189.95m, total);
    }

    [Fact]
    public void CreateOrderRequest_EmptyItems_IsAllowed()
    {
        var req = new CreateOrderRequest([]);

        Assert.NotNull(req);
        Assert.Empty(req.Items);
    }
}

public class OrderModelTests
{
    [Fact]
    public void Order_DefaultId_IsNotEmpty()
    {
        var order = new Order();

        Assert.NotEqual(Guid.Empty, order.Id);
    }

    [Fact]
    public void Order_DefaultStatus_IsNotEmpty()
    {
        var order = new Order();

        Assert.NotNull(order.Status);
    }

    [Fact]
    public void OrderItem_DefaultId_IsNotEmpty()
    {
        var item = new OrderItem();

        Assert.NotEqual(Guid.Empty, item.Id);
    }
}
