using Frontend.Models.ViewModels;
using Frontend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers;

public class OrdersController(ApiGatewayClient gateway, AuthSessionService auth) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (!auth.IsAuthenticated()) return RedirectToAction("Login", "Account");
        var orders = await gateway.GetMyOrdersAsync();
        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Place()
    {
        if (!auth.IsAuthenticated()) return RedirectToAction("Login", "Account");
        var products = await gateway.GetProductsAsync();
        return View(new PlaceOrderViewModel { Products = products });
    }

    [HttpPost]
    public async Task<IActionResult> Place(PlaceOrderViewModel model)
    {
        if (!auth.IsAuthenticated()) return RedirectToAction("Login", "Account");

        var products = await gateway.GetProductsAsync();

        // Remove empty lines (no product selected)
        var validLines = model.Lines
            .Where(l => l.ProductId != Guid.Empty && l.Quantity > 0)
            .ToList();

        if (!validLines.Any())
        {
            ModelState.AddModelError(string.Empty, "Please select at least one product.");
            model.Products = products;
            return View(model);
        }

        var lines = validLines.Select(l =>
        {
            var product = products.First(p => p.Id == l.ProductId);
            return (l.ProductId, product.Name, l.Quantity, product.Price);
        }).ToList();

        var (success, error) = await gateway.PlaceOrderAsync(lines);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to place order.");
            model.Products = products;
            return View(model);
        }

        TempData["Success"] = "Order placed! A notification has been sent asynchronously via RabbitMQ.";
        return RedirectToAction("Index");
    }
}
