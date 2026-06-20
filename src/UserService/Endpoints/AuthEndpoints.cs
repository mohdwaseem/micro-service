using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTOs;
using UserService.Models;
using UserService.Services;

namespace UserService.Endpoints;

public static class AuthEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Register a new user account")
            .WithDescription("Creates a new user with a hashed password. Returns a JWT on success.")
            .AllowAnonymous();

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Log in and receive a JWT")
            .WithDescription("Validates credentials and returns a signed JWT for subsequent API calls.")
            .AllowAnonymous();
    }

    private static async Task<IResult> Register(
        RegisterRequest req, UserDbContext db, TokenService tokenService)
    {
        if (await db.Users.AnyAsync(u => u.Email == req.Email.ToLower()))
            return Results.Conflict(new { message = "Email is already registered." });

        var user = new User
        {
            Email = req.Email.ToLower().Trim(),
            FirstName = req.FirstName.Trim(),
            LastName = req.LastName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token = tokenService.GenerateToken(user);
        return Results.Created($"/api/users/{user.Id}",
            new AuthResponse(token, user.Email, user.FirstName, user.LastName, user.Id));
    }

    private static async Task<IResult> Login(
        LoginRequest req, UserDbContext db, TokenService tokenService)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == req.Email.ToLower());

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Results.Unauthorized();

        var token = tokenService.GenerateToken(user);
        return Results.Ok(new AuthResponse(token, user.Email, user.FirstName, user.LastName, user.Id));
    }
}
