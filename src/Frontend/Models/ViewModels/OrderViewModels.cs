using System.ComponentModel.DataAnnotations;

namespace Frontend.Models.ViewModels;

public class OrderViewModel
{
    public Guid Id { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<OrderItemViewModel> Items { get; set; } = [];
}

public class OrderItemViewModel
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class PlaceOrderViewModel
{
    public List<ProductViewModel> Products { get; set; } = [];
    public List<OrderLineInput> Lines { get; set; } = [new()];
}

public class OrderLineInput
{
    [Required]
    public Guid ProductId { get; set; }
    [Required, Range(1, 100)]
    public int Quantity { get; set; } = 1;
}
