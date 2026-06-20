using Frontend.Models.ViewModels;
using Frontend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers;

public class AccountController(ApiGatewayClient gateway, AuthSessionService auth) : Controller
{
    [HttpGet]
    public IActionResult Login() => auth.IsAuthenticated() ? RedirectToAction("Index", "Products") : View();

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, token, email, name, error) = await gateway.LoginAsync(model);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Login failed.");
            return View(model);
        }

        auth.SetToken(token!, email!, name!);
        return RedirectToAction("Index", "Products");
    }

    [HttpGet]
    public IActionResult Register() => auth.IsAuthenticated() ? RedirectToAction("Index", "Products") : View();

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, token, email, name, error) = await gateway.RegisterAsync(model);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Registration failed.");
            return View(model);
        }

        auth.SetToken(token!, email!, name!);
        return RedirectToAction("Index", "Products");
    }

    [HttpPost]
    public IActionResult Logout()
    {
        auth.Clear();
        return RedirectToAction("Login");
    }
}
