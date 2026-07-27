using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DemoTokenService>();
builder.Services.AddSingleton<ConnectionAuthState>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        // Keep the JWT's original claim names ("sub", "name") instead of mapping them to
        // the legacy WS-Security URIs, so the refresh check below can read "sub" directly.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = DemoTokenService.CreateValidationParameters();
        // Browser clients using WebSockets or Server-Sent Events can't set an Authorization
        // header, so the JavaScript SignalR client sends the access token as a query string
        // parameter instead. (The .NET client used by SignalRClient in this repo *does* send
        // an Authorization header, so it never takes this path.) Read the query string token
        // only for the hub path. IMPORTANT: use HTTPS in production — query strings are
        // frequently written to server/proxy logs. This sample uses plain HTTP for
        // localhost convenience only. See
        // https://learn.microsoft.com/aspnet/core/signalr/security#access-token-logging
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Path.StartsWithSegments("/clock") &&
                    context.Request.Query.TryGetValue("access_token", out var accessToken))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "SignalR authentication refresh demo. POST /token?user=alice, then connect to /clock.");

// DEMO ONLY — this is NOT how to authenticate users. It issues a signed JWT for
// whatever username is requested, with no credential check, so the sample can run
// without a real identity provider. Treat it as a stand-in for "the user has already
// signed in." A real app must authenticate the user (ASP.NET Core Identity, Microsoft
// Entra ID, or another IdP), issue tokens from that trusted source, and validate them
// against the provider's Authority — never mint tokens from an unauthenticated endpoint.
// It is registered only in Development so a copy of this code can't expose it when deployed.
if (app.Environment.IsDevelopment())
{
    app.MapPost("/token", (string user, DemoTokenService tokens) => Results.Ok(tokens.CreateToken(user)));
}

app.MapHub<ClockHub>("/clock", options =>
{
    // dotnet/aspnetcore #67400: let the .NET SignalR client refresh auth without reconnecting.
    options.EnableAuthenticationRefresh = true;
    options.CloseOnAuthenticationExpiration = true;
    options.OnAuthenticationRefresh = context =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("SignalRAuthRefresh");

        // IMPORTANT: enabling EnableAuthenticationRefresh opts this connection OUT of
        // SignalR's built-in "reject if the user changed" hardening — the app owns that
        // policy now. A refresh must only ever extend the SAME user's session; it must
        // never swap a live connection to a different identity, because per-connection
        // state (group memberships, Context.Items, in-flight streams) stays attached.
        var previousSub = context.PreviousUser.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var newSub = context.NewUser.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (previousSub is null || !string.Equals(previousSub, newSub, StringComparison.Ordinal))
        {
            // Returning false rejects the refresh: the endpoint responds with HTTP 403 and
            // the connection keeps its existing principal.
            logger.LogWarning(
                "Rejected authentication refresh for {ConnectionId}: subject changed {PreviousUser} -> {NewUser}",
                context.ConnectionId,
                previousSub ?? "<none>",
                newSub ?? "<none>");
            return ValueTask.FromResult(false);
        }

        logger.LogInformation(
            "Accepted authentication refresh for {ConnectionId} as {User}; new expiration {NewExpiration:O}",
            context.ConnectionId,
            newSub,
            context.NewExpiration);

        // This is also where a real app would enforce any additional per-connection policy
        // (for example, re-check that the user is still permitted, or deny refresh after
        // some absolute session limit).
        return ValueTask.FromResult(true);
    };
});

app.Run();

[Authorize]
public sealed class ClockHub(ILogger<ClockHub> logger, ConnectionAuthState authState) : Hub
{
    public override Task OnConnectedAsync()
    {
        authState.Set(Context.ConnectionId, Context.User);
        logger.LogInformation(
            "Connected {ConnectionId} as {User}; token expires {TokenExpiration:O}",
            Context.ConnectionId,
            Context.User?.Identity?.Name,
            GetExpiration(Context.User));
        return base.OnConnectedAsync();
    }

    public override Task OnAuthenticationRefreshedAsync()
    {
        authState.Set(Context.ConnectionId, Context.User);
        logger.LogInformation(
            "Hub observed refreshed authentication for {ConnectionId} as {User}; token expires {TokenExpiration:O}",
            Context.ConnectionId,
            Context.User?.Identity?.Name,
            GetExpiration(Context.User));
        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        authState.Remove(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public async IAsyncEnumerable<ClockTick> StreamTicks(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var current = authState.Get(Context.ConnectionId);
            yield return new ClockTick(
                DateTimeOffset.UtcNow,
                Context.ConnectionId,
                current.UserName,
                current.TokenExpiresAt);

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }

    internal static DateTimeOffset? GetExpiration(ClaimsPrincipal? user)
    {
        var exp = user?.FindFirstValue(JwtRegisteredClaimNames.Exp);
        return long.TryParse(exp, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
    }
}

public sealed record ClockTick(
    DateTimeOffset ServerTime,
    string ConnectionId,
    string UserName,
    DateTimeOffset? TokenExpiresAt);

public sealed class ConnectionAuthState
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, AuthSnapshot> _connections = new();

    public void Set(string connectionId, ClaimsPrincipal? user)
    {
        _connections[connectionId] = new AuthSnapshot(
            user?.Identity?.Name ?? "<anonymous>",
            ClockHub.GetExpiration(user));
    }

    public AuthSnapshot Get(string connectionId) =>
        _connections.TryGetValue(connectionId, out var snapshot)
            ? snapshot
            : new AuthSnapshot("<unknown>", null);

    public void Remove(string connectionId) => _connections.TryRemove(connectionId, out _);
}

public sealed record AuthSnapshot(string UserName, DateTimeOffset? TokenExpiresAt);

public sealed record TokenResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    int ExpiresInSeconds);

// DEMO ONLY token issuer/validator. It self-issues JWTs so the sample is self-contained
// and needs no external identity provider. The signing key is generated per process with
// RandomNumberGenerator, so there is NO key material checked into this repo — the same
// approach the SignalR JwtSample in dotnet/aspnetcore uses
// (src/SignalR/samples/JwtSample/Startup.cs). Because the key is new on every run,
// tokens from a previous run stop validating when the server restarts.
// A real app must NOT self-issue tokens like this: authenticate against an IdP and set
// JwtBearerOptions.Authority to validate tokens from that trusted source.
public sealed class DemoTokenService
{
    private const string Issuer = "SignalRAuthRefreshDemo";
    private const string Audience = "SignalRAuthRefreshDemoClient";
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(45);
    private static readonly SymmetricSecurityKey SigningKey = new(RandomNumberGenerator.GetBytes(32));

    public TokenResponse CreateToken(string user)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.Add(Lifetime);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user),
            new Claim(JwtRegisteredClaimNames.Name, user),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = new ClaimsIdentity(claims),
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        return new TokenResponse(handler.CreateEncodedJwt(descriptor), expires, (int)Lifetime.TotalSeconds);
    }

    public static TokenValidationParameters CreateValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = true,
        ValidAudience = Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = SigningKey,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = JwtRegisteredClaimNames.Name
    };
}