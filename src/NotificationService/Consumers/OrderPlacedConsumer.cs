using MassTransit;
using SharedKernel.Events;

namespace NotificationService.Consumers;

/// <summary>
/// Consumes OrderPlacedEvent messages from RabbitMQ.
///
/// Teaching point: This consumer has NO knowledge of OrderService.
/// It only knows the shared event contract (OrderPlacedEvent from SharedKernel).
/// MassTransit manages the RabbitMQ connection, queue subscription, and message deserialization.
///
/// In a real application, this would call SendGrid, Twilio, etc.
/// For this demo, we log the notification to show the async event was received.
/// </summary>
public class OrderPlacedConsumer(ILogger<OrderPlacedConsumer> logger) : IConsumer<OrderPlacedEvent>
{
    public Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var evt = context.Message;

        logger.LogInformation(
            "====================================================");
        logger.LogInformation(
            "EMAIL SENT (simulated)");
        logger.LogInformation(
            "  To:      {Email}", evt.UserEmail);
        logger.LogInformation(
            "  Subject: Order #{OrderId} Confirmed", evt.OrderId);
        logger.LogInformation(
            "  Items:   {ItemCount} item(s)", evt.Items.Count);

        foreach (var item in evt.Items)
        {
            logger.LogInformation(
                "           - {ProductName} x{Qty} @ {Price:C}",
                item.ProductName, item.Quantity, item.UnitPrice);
        }

        logger.LogInformation(
            "  Total:   {Total:C}", evt.TotalAmount);
        logger.LogInformation(
            "  Placed:  {PlacedAt:u}", evt.PlacedAt);
        logger.LogInformation(
            "====================================================");

        return Task.CompletedTask;
    }
}
