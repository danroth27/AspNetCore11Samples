using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace SignalRFeatures;

public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt);

// DEMO ONLY. This self-issues JWTs so the sample needs no external identity provider, and the
// signing key is generated per process so there is no key material in this repo. Tokens issued by
// a previous run stop validating when the server restarts.
//
// A real app must NOT mint its own tokens: authenticate against an identity provider (ASP.NET Core
// Identity, Microsoft Entra ID, or another IdP) and set JwtBearerOptions.Authority so tokens are
// validated against that trusted source.
public sealed class DemoTokenService
{
    // MapInboundClaims is disabled on the server, so claims keep the names used here.
    public const string RoleClaimType = "role";

    private const string Issuer = "SignalRAuthRefreshDemo";
    private const string Audience = "SignalRAuthRefreshDemoClient";

    // Deliberately short so a refresh happens during a short demo run. The client's refresh timer
    // has a 30 second floor, so lifetimes much below this leave no room to refresh before expiry.
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(45);
    private static readonly SymmetricSecurityKey SigningKey = new(RandomNumberGenerator.GetBytes(32));
    private static readonly JsonWebTokenHandler Handler = new();

    public TokenResponse CreateToken(string user, IEnumerable<string> roles)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(Lifetime);
        var identity = new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, user),
            new Claim(JwtRegisteredClaimNames.Name, user)
        ]);

        identity.AddClaims(roles.Select(role => new Claim(RoleClaimType, role)));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = identity,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
        };

        return new TokenResponse(Handler.CreateToken(descriptor), expiresAt);
    }

    public static TokenValidationParameters CreateValidationParameters() => new()
    {
        ValidIssuer = Issuer,
        ValidAudience = Audience,
        IssuerSigningKey = SigningKey,
        NameClaimType = JwtRegisteredClaimNames.Name,
        RoleClaimType = RoleClaimType,

        // No clock skew allowance, so token expiry lines up with what the demo prints.
        ClockSkew = TimeSpan.Zero
    };
}
