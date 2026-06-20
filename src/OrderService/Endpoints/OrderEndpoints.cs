using System.Security.Claims;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Models;
using SharedKernel.Events;

namespace OrderService.Endpoints;

public static class OrderEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders").RequireAuthorization();

        group.MapGet("/", GetMyOrders)
            .WithName("GetMyOrders")
            .WithSummary("Get all orders for the authenticated user");

        group.MapGet("/{id:guid}", GetOrderById)
            .WithName("GetOrderById")
            .WithSummary("Get a specific order by ID");

        group.MapPost("/", PlaceOrder)
            .WithName("PlaceOrder")
            .WithSummary("Place a new order")
            .WithDescription(
                "Saves the order to OrderServiceDb, then publishes an OrderPlacedEvent to RabbitMQ. " +
                "Returns 201 immediately — the notification is sent asynchronously by NotificationService. " +
                "This demonstrates the async fire-and-forget messaging pattern.");
    }

    private static async Task<IResult> GetMyOrders(OrderDbContext db, ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var orders = await db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Results.Ok(orders);
    }

    private static async Task<IResult> GetOrderById(Guid id, OrderDbContext db, ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await db.Orders
            .Include(o => o.Items)
            .SingleOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        return order is null ? Results.NotFound() : Results.Ok(order);
    }

    private static async Task<IResult> PlaceOrder(
        CreateOrderRequest req,
        OrderDbContext db,
        IPublishEndpoint publishEndpoint,  // MassTransit injection — sends to RabbitMQ
        ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userEmail = user.FindFirstValue(ClaimTypes.Email)!;

        // 1. Build and save the order
        var order = new Order
        {
            UserId = userId,
            UserEmail = userEmail,
            Items = req.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // 2. Publish event to RabbitMQ — fire and forget.
        // OrderService does NOT know NotificationService exists.
        // It only knows about the shared event contract (OrderPlacedEvent).
        // MassTransit routes the message to whoever is subscribed.
        await publishEndpoint.Publish(new OrderPlacedEvent(
            OrderId: order.Id,
            UserId: order.UserId,
            UserEmail: order.UserEmail,
            Items: order.Items.Select(i => new OrderPlacedEvent.OrderItem(
                i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList(),
            TotalAmount: order.TotalAmount,
            PlacedAt: order.CreatedAt));

        // 3. Return 201 immediately — don't wait for the notification to be sent
        return Results.Created($"/api/orders/{order.Id}", order);
    }
}
