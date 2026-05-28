namespace BlazorFeatures.Data;

// Backing service for the /async-validation demo. Simulates a slow user repository
// (database lookup or remote API) so the spinner / pending UI is visible. Use a
// singleton so 'admin' / 'admin@example.com' look 'taken' across the whole demo.
public sealed class UserService
{
    private static readonly HashSet<string> RegisteredEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin@example.com",
        "test@example.com",
        "user@example.com",
    };

    private static readonly HashSet<string> RegisteredUsernames = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin",
        "root",
        "blazor",
        "danroth",
    };

    public async Task<bool> IsEmailTakenAsync(string email, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1500, cancellationToken);
        return RegisteredEmails.Contains(email);
    }

    public async Task<bool> IsUsernameTakenAsync(string username, CancellationToken cancellationToken = default)
    {
        await Task.Delay(2000, cancellationToken);
        return RegisteredUsernames.Contains(username);
    }
}
