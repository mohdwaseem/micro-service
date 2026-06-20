namespace OrderService.DTOs;

public record CreateOrderRequest(List<OrderLineRequest> Items);

public record OrderLineRequest(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);
