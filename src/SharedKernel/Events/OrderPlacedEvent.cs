namespace SharedKernel.Events;

/// <summary>
/// Published by OrderService to RabbitMQ when a new order is placed.
/// NotificationService subscribes to this event and sends a confirmation email.
///
/// Teaching point: This record is the "contract" between services.
/// Both publisher and consumer reference this same type from SharedKernel.
/// Neither service knows about the other — they only share this contract.
/// </summary>
public record OrderPlacedEvent(
    Guid OrderId,
    Guid UserId,
    string UserEmail,
    List<OrderPlacedEvent.OrderItem> Items,
    decimal TotalAmount,
    DateTime PlacedAt)
{
    public record OrderItem(
        Guid ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice);
}
