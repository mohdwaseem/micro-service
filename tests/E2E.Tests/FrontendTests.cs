// E2E tests — run against the live stack (docker compose up).
// Excluded from CI unit-test stage via: --filter "Category!=E2E"
//
// Before first run, install the Playwright browser binaries:
//   cd tests/E2E.Tests
//   dotnet build
//   pwsh bin/Debug/net10.0/playwright.ps1 install chromium
//
// Then run the full stack and execute:
//   dotnet test tests/E2E.Tests --filter "Category=E2E"

using Microsoft.Playwright;

namespace E2E.Tests;

[Trait("Category", "E2E")]
public class FrontendTests : IAsyncLifetime
{
    private IPlaywright _playwright = null!;
    private IBrowser    _browser    = null!;
    private IPage       _page       = null!;

    private const string BaseUrl = "http://localhost:3000";

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser    = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
        _page = await _browser.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }

    // ── Navigation ───────────────────────────────────────────────────────────

    [Fact]
    public async Task HomePage_Loads_WithoutServerError()
    {
        var response = await _page.GotoAsync(BaseUrl);

        Assert.NotNull(response);
        Assert.True(response!.Status < 500,
            $"Expected non-5xx status, got {response.Status}");
    }

    [Fact]
    public async Task LoginPage_Returns_200()
    {
        var response = await _page.GotoAsync($"{BaseUrl}/Account/Login");

        Assert.NotNull(response);
        Assert.Equal(200, response!.Status);
    }

    [Fact]
    public async Task RegisterPage_Returns_200()
    {
        var response = await _page.GotoAsync($"{BaseUrl}/Account/Register");

        Assert.NotNull(response);
        Assert.Equal(200, response!.Status);
    }

    // ── Login form ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginPage_HasEmailAndPasswordInputs()
    {
        await _page.GotoAsync($"{BaseUrl}/Account/Login");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        var emailInput    = await _page.QuerySelectorAsync("input#Email");
        var passwordInput = await _page.QuerySelectorAsync("input#Password");
        var submitButton  = await _page.QuerySelectorAsync("button[type='submit']");

        Assert.NotNull(emailInput);
        Assert.NotNull(passwordInput);
        Assert.NotNull(submitButton);
    }

    [Fact]
    public async Task LoginPage_InvalidCredentials_ShowsError()
    {
        await _page.GotoAsync($"{BaseUrl}/Account/Login");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await _page.FillAsync("input#Email",    "nobody@example.com");
        await _page.FillAsync("input#Password", "wrongpassword");
        await _page.ClickAsync("button[type='submit']");

        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Should stay on login page (not redirect to products)
        Assert.Contains("/Account/Login", _page.Url, StringComparison.OrdinalIgnoreCase);
    }

    // ── Register form ────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterPage_HasRequiredInputFields()
    {
        await _page.GotoAsync($"{BaseUrl}/Account/Register");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        var emailInput = await _page.QuerySelectorAsync("input#Email");
        Assert.NotNull(emailInput);
    }

    // ── Authenticated routes ─────────────────────────────────────────────────

    [Fact]
    public async Task ProductsPage_WhenUnauthenticated_RedirectsToLogin()
    {
        // Start fresh — no session cookie
        var context = await _browser.NewContextAsync();
        var page    = await context.NewPageAsync();

        await page.GotoAsync($"{BaseUrl}/Products");
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        Assert.Contains("Login", page.Url, StringComparison.OrdinalIgnoreCase);

        await context.DisposeAsync();
    }
}
