namespace Frontend.Services;

/// <summary>
/// Stores and retrieves the JWT in the server-side ASP.NET Core session.
///
/// Teaching point: By storing the JWT server-side (in ISession), the token never
/// travels to the browser as a readable value. The browser only gets a session cookie.
/// This is safer than storing JWTs in localStorage, which is accessible to JavaScript
/// (and thus to XSS attacks).
/// </summary>
public class AuthSessionService(IHttpContextAccessor httpContextAccessor)
{
    private const string TokenKey = "jwt_token";
    private const string EmailKey = "user_email";
    private const string NameKey = "user_name";

    private ISession Session => httpContextAccessor.HttpContext!.Session;

    public void SetToken(string token, string email, string fullName)
    {
        Session.SetString(TokenKey, token);
        Session.SetString(EmailKey, email);
        Session.SetString(NameKey, fullName);
    }

    public string? GetToken() => Session.GetString(TokenKey);

    public string? GetEmail() => Session.GetString(EmailKey);

    public string? GetUserName() => Session.GetString(NameKey);

    public bool IsAuthenticated() => GetToken() is not null;

    public void Clear() => Session.Clear();
}
