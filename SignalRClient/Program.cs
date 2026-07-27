using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = GetOption(args, "--server") ?? "http://localhost:5110";
var user = GetOption(args, "--user") ?? "alice";
var refreshAs = GetOption(args, "--refresh-as");
var serverProject = GetOption(args, "--server-project") ?? UserJwts.FindServerProject();
var runSeconds = int.TryParse(GetOption(args, "--duration-seconds"), out var parsedSeconds) ? parsedSeconds : 75;
var noRefresh = args.Contains("--no-refresh", StringComparer.OrdinalIgnoreCase);

var tokenSource = new UserJwts(serverProject, user, refreshAs);
var refreshes = 0;
var refreshFailures = 0;
var closed = false;
var reconnects = 0;
var shuttingDown = false;

Console.WriteLine($"SignalR auth-refresh client connecting to {serverUrl}/clock as '{user}'.");
Console.WriteLine($"Tokens come from `dotnet user-jwts` against {serverProject}.");
Console.WriteLine(noRefresh
    ? "Authentication refresh is DISABLED; the server should close the connection at token expiry."
    : "Authentication refresh is ENABLED; the connection should survive token expiry.");
if (refreshAs is not null)
{
    Console.WriteLine(
        $"Refresh tokens will be issued for a DIFFERENT user ('{refreshAs}'); the server should reject the refresh with 403.");
}

var connectionBuilder = new HubConnectionBuilder()
    .WithAuthenticationRefresh(options =>
    {
        options.EnableAutoRefresh = !noRefresh;
        options.RefreshBeforeExpiration = TimeSpan.FromSeconds(10);
        options.OnAuthenticationRefreshed = context =>
        {
            refreshes++;
            Console.WriteLine($"*** AUTH REFRESHED #{refreshes} at {DateTimeOffset.Now:HH:mm:ss}; reported lifetime {context.NewTokenLifetime} ***");
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
        options.AccessTokenProvider = async () => await tokenSource.GetAccessTokenAsync();
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

// Tokens come from `dotnet user-jwts`, the development-time JWT tool, instead of a
// token-issuing endpoint in the sample. The signing key lives in the server project's
// user secrets, so no key material is checked into this repo. In a real app this is
// where you'd acquire a token from your identity provider (MSAL, an OIDC client, ...).
sealed class UserJwts(string serverProject, string user, string? refreshAs = null)
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromSeconds(45);
    private string? _current;
    private DateTimeOffset _currentExpiresAt;
    private int _issued;

    public async Task<string> GetAccessTokenAsync()
    {
        // SignalR asks for a token on every HTTP request it makes (negotiate, then the
        // transport connect), and again when refreshing. Reuse the current token until it
        // is close to expiry so a refresh gets a genuinely longer-lived one.
        if (_current is not null && _currentExpiresAt > DateTimeOffset.UtcNow.AddSeconds(15))
        {
            return _current;
        }

        // --refresh-as requests later tokens for a different user so the server's
        // OnAuthenticationRefresh identity check can be demonstrated rejecting them.
        var tokenUser = _issued == 0 ? user : refreshAs ?? user;

        var psi = new System.Diagnostics.ProcessStartInfo("dotnet")
        {
            ArgumentList =
            {
                "user-jwts", "create",
                "--project", serverProject,
                "--name", tokenUser,
                "--valid-for", $"{(int)TokenLifetime.TotalSeconds}s",
                "--output", "token",
            },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException("Could not start the dotnet CLI.");
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"`dotnet user-jwts create` failed ({process.ExitCode}). Run the one-time setup in the README. {stderr.Trim()}");
        }

        _current = stdout.Trim();
        _currentExpiresAt = DateTimeOffset.UtcNow.Add(TokenLifetime);
        _issued++;
        Console.WriteLine($"[token] user-jwts issued #{_issued} for '{tokenUser}', valid for {TokenLifetime.TotalSeconds:F0}s");
        return _current;
    }

    // The client runs `dotnet user-jwts` against the server project, so locate it whether
    // the client was launched from the repo root or from its own directory.
    public static string FindServerProject()
    {
        for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "SignalRFeatures");
            if (File.Exists(Path.Combine(candidate, "SignalRFeatures.csproj")))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            "Could not find the SignalRFeatures project. Pass --server-project <path>.");
    }
}

sealed record ClockTick(DateTimeOffset ServerTime, string ConnectionId, string UserName, DateTimeOffset? TokenExpiresAt);