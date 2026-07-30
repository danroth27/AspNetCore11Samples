using System.Collections.Concurrent;

namespace SignalRFeatures;

// Stands in for wherever an app looks up a user's current permissions when it issues a token
// (a database, a directory, an entitlements service). Promoting a user changes the claims in the
// next token they are issued; it does not touch any connection they already have open.
public sealed class UserDirectory
{
    private static readonly string[] StandardRoles = ["user"];
    private static readonly string[] AdminRoles = ["user", "admin"];

    private readonly ConcurrentDictionary<string, byte> _admins = new(StringComparer.OrdinalIgnoreCase);

    public void Promote(string user) => _admins[user] = 0;

    // Lets the demo be run repeatedly against one server process: the client resets the user at
    // startup so the promotion is always visible, rather than carrying over from an earlier run.
    public void Reset(string user) => _admins.TryRemove(user, out _);

    public string[] GetRoles(string user) => _admins.ContainsKey(user) ? AdminRoles : StandardRoles;
}
