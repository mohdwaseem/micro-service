/// <summary>
/// ApiGateway — the single entry point for all client requests.
/// Uses YARP (Yet Another Reverse Proxy) to route requests to downstream microservices.
///
/// Teaching points:
///   1. Single entry point: the MVC Frontend (and any other client) only needs one URL.
///   2. JWT validation at the gateway = first line of defense. Unauthenticated requests
///      to protected routes are rejected here before reaching downstream services.
///   3. JWT is forwarded to downstream services, which also validate it (defense-in-depth).
///   4. /api/auth/* routes are anonymous — login and register need no token.
///   5. CORS is configured here (not in individual services) since the gateway is the
///      public surface area that browsers talk to.
/// </summary>

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// === Services ===

// YARP reads routing config from the "ReverseProxy" section of appsettings.json.
// It creates typed routes and clusters that YARP uses to forward requests.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// JWT validation — same secret, issuer, and audience as all backend services.
// The gateway rejects invalid/missing tokens before forwarding to downstream.
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

// CORS — allow the MVC Frontend origin to make API calls through the gateway.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(
                builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// === Middleware ===
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// YARP handles all routing — no MapGet/MapPost needed here.
// Route authorization policies are defined in appsettings.json per route.
app.MapReverseProxy();

app.Run();
