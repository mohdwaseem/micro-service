namespace OrderService.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = [];
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Placed";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
