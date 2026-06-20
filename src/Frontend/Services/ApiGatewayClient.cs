using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Frontend.Models.ViewModels;

namespace Frontend.Services;

/// <summary>
/// Typed HTTP client that forwards all requests to the API Gateway.
/// Reads the JWT from the server-side session and attaches it as a Bearer token.
///
/// Teaching point: The Frontend only knows ONE URL — the API Gateway.
/// It has no awareness of UserService, ProductService, or OrderService.
/// All routing decisions live in the gateway's appsettings.json.
/// </summary>
public class ApiGatewayClient(HttpClient httpClient, AuthSessionService auth)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private void AttachToken()
    {
        var token = auth.GetToken();
        if (token is not null)
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
    }

    // === Auth ===

    public async Task<(bool Success, string? Token, string? Email, string? FullName, string? Error)>
        RegisterAsync(RegisterViewModel model)
    {
        var payload = new { model.FirstName, model.LastName, model.Email, model.Password };
        var response = await httpClient.PostAsync("/api/auth/register",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            return (false, null, null, null, err);
        }

        var data = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), JsonOptions);
        return (true,
            data.GetProperty("token").GetString(),
            data.GetProperty("email").GetString(),
            $"{data.GetProperty("firstName").GetString()} {data.GetProperty("lastName").GetString()}",
            null);
    }

    public async Task<(bool Success, string? Token, string? Email, string? FullName, string? Error)>
        LoginAsync(LoginViewModel model)
    {
        var payload = new { model.Email, model.Password };
        var response = await httpClient.PostAsync("/api/auth/login",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
            return (false, null, null, null, "Invalid email or password.");

        var data = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), JsonOptions);
        return (true,
            data.GetProperty("token").GetString(),
            data.GetProperty("email").GetString(),
            $"{data.GetProperty("firstName").GetString()} {data.GetProperty("lastName").GetString()}",
            null);
    }

    // === Products ===

    public async Task<List<ProductViewModel>> GetProductsAsync()
    {
        var response = await httpClient.GetAsync("/api/products");
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<List<ProductViewModel>>(
            await response.Content.ReadAsStringAsync(), JsonOptions) ?? [];
    }

    // === Orders ===

    public async Task<List<OrderViewModel>> GetMyOrdersAsync()
    {
        AttachToken();
        var response = await httpClient.GetAsync("/api/orders");
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<List<OrderViewModel>>(
            await response.Content.ReadAsStringAsync(), JsonOptions) ?? [];
    }

    public async Task<(bool Success, string? Error)> PlaceOrderAsync(
        List<(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice)> lines)
    {
        AttachToken();
        var payload = new
        {
            Items = lines.Select(l => new
            {
                ProductId = l.ProductId,
                ProductName = l.ProductName,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice
            })
        };

        var response = await httpClient.PostAsync("/api/orders",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await response.Content.ReadAsStringAsync());
    }
}
