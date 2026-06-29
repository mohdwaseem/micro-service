using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UserService.Models;
using UserService.Services;

namespace UserService.Tests;

public class TokenServiceTests
{
    private readonly TokenService _sut;
    private readonly User _user;

    public TokenServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"]   = "unit-test-secret-minimum-32-characters-long!",
                ["Jwt:Issuer"]   = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
            })
            .Build();

        _sut = new TokenService(config);

        _user = new User
        {
            Id           = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email        = "jane.doe@example.com",
            FirstName    = "Jane",
            LastName     = "Doe",
            PasswordHash = "irrelevant",
        };
    }

    [Fact]
    public void GenerateToken_Returns_NonEmptyString()
    {
        var token = _sut.GenerateToken(_user);

        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateToken_Returns_ThreePartJwtStructure()
    {
        var token = _sut.GenerateToken(_user);

        // A valid compact JWT is header.payload.signature
        Assert.Equal(3, token.Split('.').Length);
    }

    [Fact]
    public void GenerateToken_Token_IsVerifiableWithCorrectKey()
    {
        var token = _sut.GenerateToken(_user);

        var handler = new JwtSecurityTokenHandler();
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                          Encoding.UTF8.GetBytes(
                                              "unit-test-secret-minimum-32-characters-long!")),
            ValidIssuer              = "TestIssuer",
            ValidAudience            = "TestAudience",
            ValidateLifetime         = true,
        };

        var principal = handler.ValidateToken(token, validationParams, out var _);
        Assert.NotNull(principal);
    }

    [Fact]
    public void GenerateToken_Token_ContainsEmailClaim()
    {
        var token = _sut.GenerateToken(_user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var emailClaim = jwt.Claims.FirstOrDefault(c => c.Value == _user.Email);

        Assert.NotNull(emailClaim);
    }

    [Fact]
    public void GenerateToken_Token_ContainsUserIdClaim()
    {
        var token = _sut.GenerateToken(_user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var subClaim = jwt.Claims.FirstOrDefault(c => c.Value == _user.Id.ToString());

        Assert.NotNull(subClaim);
    }

    [Fact]
    public void GenerateToken_Expiry_IsApproximatelyEightHoursFromNow()
    {
        var before = DateTime.UtcNow;
        var token  = _sut.GenerateToken(_user);
        var after  = DateTime.UtcNow;

        var jwt    = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var expiry = jwt.ValidTo;

        Assert.InRange(expiry,
            before.AddHours(7).AddMinutes(59),
            after.AddHours(8).AddMinutes(1));
    }

    [Fact]
    public void GenerateToken_DifferentUsers_ProduceDifferentTokens()
    {
        var otherUser = new User
        {
            Id        = Guid.NewGuid(),
            Email     = "other@example.com",
            FirstName = "Other",
            LastName  = "User",
        };

        var token1 = _sut.GenerateToken(_user);
        var token2 = _sut.GenerateToken(otherUser);

        Assert.NotEqual(token1, token2);
    }
}
