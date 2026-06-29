/// <summary>
/// UserService — responsible for user registration, login, and JWT issuance.
/// This is the only service that ISSUES JWTs. All other services only VALIDATE them.
/// Database: UserServiceDb (SQL Server) — owns user identity data exclusively.
/// </summary>

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using UserService.Data;
using UserService.Endpoints;
using UserService.Services;

var builder = WebApplication.CreateBuilder(args);

// === Services ===
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<TokenService>();

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

// === Database Migration ===
// Development: EF Core handles schema creation for a smooth local dev experience.
// Production/CI: DbUp migrator runs before the container starts (see Jenkinsfile DB Migrate stage).
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    db.Database.Migrate();
}

// === Middleware ===
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "User Service API";
    options.Theme = ScalarTheme.Moon;
});

app.UseAuthentication();
app.UseAuthorization();

// === Endpoints ===
AuthEndpoints.Map(app);

app.Run();

// Enables WebApplicationFactory<Program> in integration tests
public partial class Program { }
