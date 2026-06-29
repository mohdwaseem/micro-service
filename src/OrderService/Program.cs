/// <summary>
/// OrderService — manages order creation and history.
/// KEY TEACHING POINT: After saving an order, it publishes an OrderPlacedEvent
/// to RabbitMQ via MassTransit. NotificationService consumes this event asynchronously.
/// OrderService does NOT know NotificationService exists — pure decoupling.
/// Database: OrderServiceDb (SQL Server) — owns order data exclusively.
/// Scalar API docs: /scalar
/// </summary>

using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderService.Data;
using OrderService.Endpoints;
using Scalar.AspNetCore;
using SharedKernel.Messaging;

var builder = WebApplication.CreateBuilder(args);

// === Services ===
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secret = builder.Configuration["Jwt:Secret"]!;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };
    });

builder.Services.AddAuthorization();

// Register MassTransit with RabbitMQ — no consumers here, only a publisher.
// IPublishEndpoint is injected directly into the endpoint handler.
builder.Services.AddRabbitMqBus(builder.Configuration);

builder.Services.AddOpenApi();

var app = builder.Build();

// === Database Migration ===
// Development: EF Core handles schema creation for a smooth local dev experience.
// Production/CI: DbUp migrator runs before the container starts (see Jenkinsfile DB Migrate stage).
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.Migrate();
}

// === Middleware ===
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "Order Service API";
    options.Theme = ScalarTheme.Moon;
});

app.UseAuthentication();
app.UseAuthorization();

// === Endpoints ===
OrderEndpoints.Map(app);

app.Run();

// Enables WebApplicationFactory<Program> in integration tests
public partial class Program { }
