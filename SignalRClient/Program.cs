using Microsoft.AspNetCore.SignalR.Client;
using SignalRClient;

var options = ClientOptions.Parse(args);

Console.WriteLine($"Connecting to {options.ServerUrl}/clock as '{options.User}'.");
Console.WriteLine(options.AutoRefresh
    ? "Authentication refresh is ENABLED: the connection should survive token expiry."
    : "Authentication refresh is DISABLED: the server should close the connection at token expiry.");

if (options.RefreshAs is not null)
{
    Console.WriteLine($"Refresh tokens will be issued for a DIFFERENT user ('{options.RefreshAs}'): the refresh should be rejected.");
}

using var http = new HttpClient { BaseAddress = new Uri(options.ServerUrl) };
var tokens = new TokenProvider(http, options.User, options.RefreshAs);

// Clear any promotion left over from an earlier run so the demo always starts from a standard user.
using (var resetResponse = await http.PostAsync($"/reset?user={Uri.EscapeDataString(options.User)}", content: null))
{
    resetResponse.EnsureSuccessStatusCode();
}

var refreshes = 0;
var refreshFailures = 0;
var reconnects = 0;
var running = true;

// The Closed event can arrive slightly after the stream unwinds, so signal it rather than
// polling a flag.
var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{options.ServerUrl}/clock", o => o.AccessTokenProvider = async () => await tokens.GetAccessTokenAsync())
    .WithAuthenticationRefresh(o =>
    {
        // New in .NET 11. Auto-refresh is on by default and RefreshBeforeExpiration defaults to
        // 5 minutes; the demo issues 45 second tokens, so dial it down to keep the demo short.
        o.EnableAutoRefresh = options.AutoRefresh;
        o.RefreshBeforeExpiration = TimeSpan.FromSeconds(10);
    })
    .Build();

// RC1 moved refresh notifications from AuthenticationRefreshOptions to HubConnection
// events, matching the existing Closed/Reconnecting/Reconnected event pattern.
connection.AuthenticationRefreshed += context =>
{
    refreshes++;
    Console.WriteLine($"*** AUTH REFRESHED #{refreshes} at {context.RefreshedAt:HH:mm:ss}; new lifetime {context.NewTokenLifetime} ***");

    return Task.CompletedTask;
};

connection.AuthenticationRefreshFailed += context =>
{
    refreshFailures++;
    Console.WriteLine($"*** AUTH REFRESH FAILED at {DateTimeOffset.Now:HH:mm:ss}: {context.Exception.Message} ***");

    return Task.CompletedTask;
};

connection.Closed += error =>
{
    if (running)
    {
        Console.WriteLine($"*** CONNECTION CLOSED at {DateTimeOffset.Now:HH:mm:ss}: {error?.Message ?? "no error"} ***");
        closed.TrySetResult();
    }

    return Task.CompletedTask;
};

connection.Reconnecting += error =>
{
    reconnects++;
    Console.WriteLine($"*** RECONNECTING at {DateTimeOffset.Now:HH:mm:ss}: {error?.Message ?? "no error"} ***");

    return Task.CompletedTask;
};

await connection.StartAsync();
Console.WriteLine($"Connected: {connection.ConnectionId}");

using var runCts = new CancellationTokenSource(options.Duration);
var ticks = 0;
var sawAdmin = false;

// Simulate the user's permissions changing while they're connected. RefreshAuthenticationAsync is
// the manual counterpart to the automatic timer: the app already knows something changed, so it
// applies the new claims immediately instead of waiting for the token to near expiry.
var promotion = options.Promote ? PromoteAsync() : Task.CompletedTask;

async Task PromoteAsync()
{
    try
    {
        await Task.Delay(options.PromoteAfter, runCts.Token);

        using var response = await http.PostAsync($"/promote?user={Uri.EscapeDataString(options.User)}", content: null, runCts.Token);
        response.EnsureSuccessStatusCode();

        Console.WriteLine($"*** PROMOTED '{options.User}' TO ADMIN at {DateTimeOffset.Now:HH:mm:ss}; refreshing in place ***");

        tokens.Invalidate();
        await connection.RefreshAuthenticationAsync();
    }
    catch (OperationCanceledException)
    {
    }
    catch (Exception ex)
    {
        Console.WriteLine($"*** PROMOTION FAILED: {ex.Message} ***");
    }
}

try
{
    await foreach (var tick in connection.StreamAsync<ClockTick>("StreamTicks", runCts.Token))
    {
        ticks++;
        sawAdmin |= tick.Roles.Contains("admin");
        Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}] tick #{ticks,2} connection={tick.ConnectionId} user={tick.UserName} roles={string.Join(",", tick.Roles)} token-exp={tick.TokenExpiresAt?.ToLocalTime():HH:mm:ss} state={connection.State}");
    }
}
catch (OperationCanceledException) when (runCts.IsCancellationRequested)
{
}
catch (Exception ex)
{
    Console.WriteLine($"Stream ended with {ex.GetType().Name}: {ex.Message}");
}

await promotion;

// If the stream ended before the run duration elapsed, something closed the connection. Give the
// Closed event a moment to arrive so the summary reflects it.
if (!runCts.IsCancellationRequested)
{
    await Task.WhenAny(closed.Task, Task.Delay(TimeSpan.FromSeconds(2)));
}

var closedByServer = closed.Task.IsCompleted;
running = false;

if (options.RefreshAs is not null)
{
    // The connection must never be re-bound to a different user, whether the server's
    // OnAuthenticationRefresh gate rejects it or SignalR aborts the connection itself.
    Report(refreshes == 0, $"the connection was never re-bound to '{options.RefreshAs}'.");
}
else if (!options.AutoRefresh)
{
    // The pre-.NET 11 experience: the token expires and the connection goes away mid-stream.
    Report(closedByServer, "the connection was closed once the token expired.");
}
else
{
    Report(refreshes > 0 && ticks > 0 && sawAdmin && !closedByServer && reconnects == 0,
        $"received {ticks} ticks across {refreshes} authentication refresh(es), picked up the new admin role, and never closed or reconnected.");
}

void Report(bool expected, string expectation) => Console.WriteLine(expected
    ? $"SUCCESS: {expectation}"
    : $"FAILURE: expected {expectation} (ticks={ticks}, refreshes={refreshes}, refreshFailures={refreshFailures}, sawAdmin={sawAdmin}, closed={closedByServer}, reconnects={reconnects})");

internal sealed record ClockTick(
    string ConnectionId,
    string UserName,
    IReadOnlyList<string> Roles,
    DateTimeOffset? TokenExpiresAt);
