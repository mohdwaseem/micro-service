using Frontend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers;

public class ProductsController(ApiGatewayClient gateway) : Controller
{
    public async Task<IActionResult> Index()
    {
        var products = await gateway.GetProductsAsync();
        return View(products);
    }
}
