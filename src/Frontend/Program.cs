/// <summary>
/// Frontend — ASP.NET Core MVC application (Razor views + Bootstrap).
/// Calls the API Gateway for all data. Stores the JWT in server-side ISession.
///
/// Teaching point: The frontend knows ONE URL — the API Gateway base URL.
/// It has no awareness of individual microservices. The gateway handles routing.
/// JWT is stored in ISession (server-side), never exposed to the browser directly.
/// </summary>

using Frontend.Services;

var builder = WebApplication.CreateBuilder(args);

// === Services ===
builder.Services.AddControllersWithViews();

// Session — stores JWT server-side (safer than browser localStorage for JWTs)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthSessionService>();

// HttpClient pointing at the API Gateway — all API calls go through here.
builder.Services.AddHttpClient<ApiGatewayClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ApiGateway:BaseUrl"] ?? "http://localhost:5000");
});

var app = builder.Build();

// === Middleware ===
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Products}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
