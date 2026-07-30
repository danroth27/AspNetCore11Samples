using System.Net.Http.Json;

namespace SignalRClient;

internal sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt);

// Stands in for whatever a real app uses to acquire access tokens (MSAL, an OIDC client, a token
// exchange with its own backend). The SignalR client calls into this for the initial connection and
// again for every refresh, so returning a freshly issued token here is all the app has to do.
internal sealed class TokenProvider(HttpClient http, string user, string? refreshAs)
{
    private TokenResponse? _current;
    private int _issued;

    // Forces the next call to fetch a new token. Needed when the user's permissions change, since
    // the cached token still carries the old claims.
    public void Invalidate() => _current = null;

    public async Task<string> GetAccessTokenAsync()
    {
        if (_current is not null && _current.ExpiresAt > DateTimeOffset.UtcNow.AddSeconds(15))
        {
            return _current.AccessToken;
        }

        // --refresh-as requests later tokens for a different user so the server can demonstrate
        // rejecting a refresh that would change the connection's identity.
        var tokenUser = _issued == 0 ? user : refreshAs ?? user;

        var response = await http.PostAsync($"/token?user={Uri.EscapeDataString(tokenUser)}", content: null);
        response.EnsureSuccessStatusCode();

        _current = await response.Content.ReadFromJsonAsync<TokenResponse>()
            ?? throw new InvalidOperationException("The token endpoint returned an empty response.");
        _issued++;

        Console.WriteLine($"[token] issued #{_issued} for '{tokenUser}'; expires {_current.ExpiresAt.ToLocalTime():HH:mm:ss}");

        return _current.AccessToken;
    }
}
