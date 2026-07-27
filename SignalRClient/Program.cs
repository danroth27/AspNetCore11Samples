using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = GetOption(args, "--server") ?? "http://localhost:5110";
var user = GetOption(args, "--user") ?? "alice";
var refreshAs = GetOption(args, "--refresh-as");
var runSeconds = int.TryParse(GetOption(args, "--duration-seconds"), out var parsedSeconds) ? parsedSeconds : 75;
var noRefresh = args.Contains("--no-refresh", StringComparer.OrdinalIgnoreCase);

using var http = new HttpClient { BaseAddress = new Uri(serverUrl) };
var tokenCache = new TokenCache(http, user, refreshAs);
var refreshes = 0;
var refreshFailures = 0;
var closed = false;
var reconnects = 0;
var shuttingDown = false;

Console.WriteLine($"SignalR auth-refresh client connecting to {serverUrl}/clock as '{user}'.");
Console.WriteLine(noRefresh
    ? "Authentication refresh is DISABLED; the server should close the connection at token expiry."
    : "Authentication refresh is ENABLED; the connection should survive token expiry.");
if (refreshAs is not null)
{
    Console.WriteLine(
        $"Refresh tokens will be issued for a DIFFERENT user ('{refreshAs}'); the server should reject the refresh with 403.");
}

var initialToken = await tokenCache.GetAccessTokenAsync(forceRefresh: true);
Console.WriteLine($"Initial token expires at {tokenCache.ExpiresAt:HH:mm:ss} UTC.");

var connectionBuilder = new HubConnectionBuilder()
    .WithAuthenticationRefresh(options =>
    {
        options.EnableAutoRefresh = !noRefresh;
        options.RefreshBeforeExpiration = TimeSpan.FromSeconds(10);
        options.OnAuthenticationRefreshed = context =>
        {
            refreshes++;
            Console.WriteLine($"*** AUTH REFRESHED #{refreshes} at {DateTimeOffset.Now:HH:mm:ss}; reported lifetime {context.NewTokenLifetime}; next token expires {tokenCache.ExpiresAt:HH:mm:ss} UTC ***");
            return Task.CompletedTask;
        };
        options.OnAuthenticationRefreshFailed = context =>
        {
            refreshFailures++;
            Console.WriteLine($"*** AUTH REFRESH FAILED at {DateTimeOffset.Now:HH:mm:ss}: {context.Exception?.Message ?? "unknown error"} ***");
            return Task.CompletedTask;
        };
    })
    .WithUrl($"{serverUrl}/clock", options =>
    {
        options.AccessTokenProvider = async () => await tokenCache.GetAccessTokenAsync();
    });
var connection = connectionBuilder.Build();
connection.Closed += error =>
{
    if (!shuttingDown)
    {
        closed = true;
    }

    Console.WriteLine($"*** CONNECTION CLOSED at {DateTimeOffset.Now:HH:mm:ss}: {error?.Message ?? "no error"} ***");
    return Task.CompletedTask;
};
connection.Reconnecting += error =>
{
    reconnects++;
    Console.WriteLine($"*** RECONNECTING at {DateTimeOffset.Now:HH:mm:ss}: {error?.Message ?? "no error"} ***");
    return Task.CompletedTask;
};
connection.Reconnected += connectionId =>
{
    Console.WriteLine($"*** RECONNECTED at {DateTimeOffset.Now:HH:mm:ss}: {connectionId} ***");
    return Task.CompletedTask;
};

await connection.StartAsync();
Console.WriteLine($"Connected: {connection.ConnectionId}");

using var runCts = new CancellationTokenSource(TimeSpan.FromSeconds(runSeconds));
var ticks = 0;
try
{
    await foreach (var tick in connection.StreamAsync<ClockTick>("StreamTicks", runCts.Token))
    {
        ticks++;
        Console.WriteLine(
            $"[{DateTimeOffset.Now:HH:mm:ss}] tick #{ticks,2} connection={tick.ConnectionId} user={tick.UserName} token-exp={tick.TokenExpiresAt:HH:mm:ss} server={tick.ServerTime:HH:mm:ss} state={connection.State}");
    }
}
catch (OperationCanceledException) when (runCts.IsCancellationRequested)
{
}
catch (Exception ex)
{
    if (!runCts.IsCancellationRequested)
    {
        closed = true;
    }

    Console.WriteLine($"Stream ended with {ex.GetType().Name}: {ex.Message}");
}

runCts.Cancel();

if (noRefresh)
{
    Console.WriteLine($"NO-REFRESH SUMMARY: ticks={ticks}, refreshes={refreshes}, closed={closed}, reconnects={reconnects}");
    shuttingDown = true;
    await connection.DisposeAsync();
    return;
}

if (refreshAs is not null)
{
    // The server rejects a refresh that changes identity, so we expect a failed refresh
    // and no successful one.
    var rejected = refreshFailures > 0 && refreshes == 0;
    Console.WriteLine(rejected
        ? $"SUCCESS: the server rejected {refreshFailures} identity-changing refresh(es); the connection was never re-bound to '{refreshAs}'."
        : $"FAILURE: expected the refresh to be rejected, but refreshes={refreshes}, refreshFailures={refreshFailures}.");
    shuttingDown = true;
    await connection.DisposeAsync();
    return;
}

var success = ticks > 0 && refreshes > 0 && !closed && reconnects == 0;
Console.WriteLine(success
    ? $"SUCCESS: received {ticks} ticks, observed {refreshes} auth refresh(es), and the connection never closed or reconnected."
    : $"FAILURE: ticks={ticks}, refreshes={refreshes}, closed={closed}, reconnects={reconnects}");
shuttingDown = true;
await connection.DisposeAsync();


static string? GetOption(string[] args, string name)
{
    var index = Array.FindIndex(args, arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

sealed class TokenCache(HttpClient http, string user, string? refreshAs = null)
{
    private TokenResponse? _current;
    private int _issued;

    public DateTimeOffset ExpiresAt => _current?.ExpiresAt ?? DateTimeOffset.MinValue;

    public async Task<string> GetAccessTokenAsync(bool forceRefresh = false)
    {
        // The auth-refresh request re-invokes AccessTokenProvider. Return a newly issued
        // token when the current one is close to expiry so the refresh carries a
        // longer-lived principal.
        if (!forceRefresh && _current is not null && _current.ExpiresAt > DateTimeOffset.UtcNow.AddSeconds(15))
        {
            return _current.AccessToken;
        }

        // --refresh-as requests later tokens for a different user so the server's
        // OnAuthenticationRefresh identity check can be demonstrated rejecting them.
        var tokenUser = _issued == 0 ? user : refreshAs ?? user;

        var response = await http.PostAsync($"/token?user={Uri.EscapeDataString(tokenUser)}", content: null);
        response.EnsureSuccessStatusCode();
        _current = await response.Content.ReadFromJsonAsync<TokenResponse>()
            ?? throw new InvalidOperationException("Token endpoint returned an empty response.");
        _issued++;
        Console.WriteLine($"[token] issued #{_issued} for '{tokenUser}'; expires {_current.ExpiresAt:HH:mm:ss} UTC");
        return _current.AccessToken;
    }
}

sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt, int ExpiresInSeconds);
sealed record ClockTick(DateTimeOffset ServerTime, string ConnectionId, string UserName, DateTimeOffset? TokenExpiresAt);