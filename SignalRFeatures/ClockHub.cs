using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;

namespace SignalRFeatures;

public sealed record ClockTick(
    string ConnectionId,
    string UserName,
    IReadOnlyList<string> Roles,
    DateTimeOffset? TokenExpiresAt);

[Authorize]
public sealed class ClockHub(ILogger<ClockHub> logger) : Hub
{
    public override Task OnConnectedAsync()
    {
        logger.LogInformation("Connected {ConnectionId} as {User} with roles [{Roles}]; token expires {Expiration:O}.",
            Context.ConnectionId, Context.User?.Identity?.Name, string.Join(", ", GetRoles(Context.User)), GetExpiration(Context.User));

        return base.OnConnectedAsync();
    }

    /// <summary>
    /// New in .NET 11. Called after a refreshed token has been validated and applied to this
    /// connection, so <see cref="Hub.Context"/> already reflects the new principal. Use it to
    /// re-evaluate anything derived from the user's claims, such as group membership.
    /// </summary>
    public override async Task OnAuthenticationRefreshedAsync()
    {
        var roles = GetRoles(Context.User);

        logger.LogInformation("Authentication refreshed for {ConnectionId} as {User} with roles [{Roles}]; token expires {Expiration:O}.",
            Context.ConnectionId, Context.User?.Identity?.Name, string.Join(", ", roles), GetExpiration(Context.User));

        // The refreshed token can carry different claims, so permission-derived state has to be
        // recomputed here rather than only at connect time.
        if (roles.Contains("admin"))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
        }
        else
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admins");
        }
    }

    public async IAsyncEnumerable<ClockTick> StreamTicks(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var connectionId = Context.ConnectionId;

        // Context.User is a snapshot taken when this method was invoked, so a long-running stream
        // would keep reporting the token the connection started with. Read the connection's
        // current principal instead so each tick shows refreshes as they happen.
        var connectionUser = Context.Features.Get<IConnectionUserFeature>();

        while (!cancellationToken.IsCancellationRequested)
        {
            var user = connectionUser?.User;

            yield return new ClockTick(
                connectionId,
                user?.Identity?.Name ?? "<anonymous>",
                GetRoles(user),
                GetExpiration(user));

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }

    private static string[] GetRoles(ClaimsPrincipal? user) =>
        user?.FindAll(DemoTokenService.RoleClaimType).Select(claim => claim.Value).ToArray() ?? [];

    private static DateTimeOffset? GetExpiration(ClaimsPrincipal? user) =>
        long.TryParse(user?.FindFirstValue(JwtRegisteredClaimNames.Exp), out var secondsSinceEpoch)
            ? DateTimeOffset.FromUnixTimeSeconds(secondsSinceEpoch)
            : null;
}
