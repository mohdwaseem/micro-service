/// <summary>
/// ProductService — manages the product catalog (CRUD).
/// Public reads (GET) are anonymous. Write operations require a valid JWT.
/// Database: ProductServiceDb (SQL Server) — owns product data exclusively.
/// Seeds 5 sample products on first startup.
/// Scalar API docs: /scalar
/// </summary>

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProductService.Data;
using ProductService.Endpoints;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// === Services ===
builder.Services.AddDbContext<ProductDbContext>(options =>
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
builder.Services.AddOpenApi();

var app = builder.Build();

// === Database Migration + Seed ===
// Development: EF Core handles schema creation for a smooth local dev experience.
// Production/CI: DbUp migrator runs before the container starts (see Jenkinsfile DB Migrate stage).
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    db.Database.Migrate();
    await SeedData.SeedAsync(db);
}

// === Middleware ===
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "Product Service API";
    options.Theme = ScalarTheme.Moon;
});

app.UseAuthentication();
app.UseAuthorization();

// === Endpoints ===
ProductEndpoints.Map(app);

app.Run();

// Enables WebApplicationFactory<Program> in integration tests
public partial class Program { }
