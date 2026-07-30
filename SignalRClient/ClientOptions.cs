namespace SignalRClient;

internal sealed record ClientOptions(
    string ServerUrl,
    string User,
    string? RefreshAs,
    bool AutoRefresh,
    TimeSpan Duration)
{
    // Long enough to see ticks before anything happens, short enough not to stall a demo.
    public TimeSpan PromoteAfter { get; } = TimeSpan.FromSeconds(8);

    // The promotion refreshes in place, which would mask the connection drop that --no-refresh
    // exists to show, and would fight the identity check that --refresh-as exercises.
    public bool Promote => AutoRefresh && RefreshAs is null;

    public static ClientOptions Parse(string[] args) => new(
        GetValue(args, "--server") ?? "https://localhost:7110",
        GetValue(args, "--user") ?? "alice",
        GetValue(args, "--refresh-as"),
        !args.Contains("--no-refresh", StringComparer.OrdinalIgnoreCase),
        TimeSpan.FromSeconds(int.TryParse(GetValue(args, "--duration-seconds"), out var seconds) ? seconds : 50));

    private static string? GetValue(string[] args, string name)
    {
        var index = Array.FindIndex(args, arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));

        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
