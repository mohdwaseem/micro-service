namespace UserService.DTOs;

public record RegisterRequest(string FirstName, string LastName, string Email, string Password);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string Token, string Email, string FirstName, string LastName, Guid UserId);
