using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
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
        options.TokenValidationParameters = DemoTokenService.CreateValidationParameters();
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

app.MapPost("/token", (string user, DemoTokenService tokens) =>
{
    var token = tokens.CreateToken(user);
    return Results.Ok(token);
});

app.MapHub<ClockHub>("/clock", options =>
{
    // dotnet/aspnetcore #67400: let the .NET SignalR client refresh auth without reconnecting.
    options.EnableAuthenticationRefresh = true;
    options.CloseOnAuthenticationExpiration = true;
    options.OnAuthenticationRefresh = context =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("SignalRAuthRefresh");
        logger.LogInformation(
            "Accepted authentication refresh for {ConnectionId}: {PreviousUser} -> {NewUser}; new expiration {NewExpiration:O}",
            context.ConnectionId,
            context.PreviousUser.Identity?.Name ?? "<anonymous>",
            context.NewUser.Identity?.Name ?? "<anonymous>",
            context.NewExpiration);

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

public sealed class DemoTokenService
{
    private const string Issuer = "SignalRAuthRefreshDemo";
    private const string Audience = "SignalRAuthRefreshDemoClient";
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(45);
    private static readonly SymmetricSecurityKey SigningKey = new(
        Encoding.UTF8.GetBytes("SignalR authentication refresh demo signing key (.NET 11)."));

    public TokenResponse CreateToken(string user)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.Add(Lifetime);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user),
            new Claim(ClaimTypes.Name, user),
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
        NameClaimType = ClaimTypes.Name
    };
}