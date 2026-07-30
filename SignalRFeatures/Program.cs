using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using SignalRFeatures;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DemoTokenService>();
builder.Services.AddSingleton<UserDirectory>();
builder.Services.AddSignalR();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep the JWT's original claim names ("sub", "name") instead of mapping them to the
        // legacy WS-Security URIs, so the refresh callback below can read "sub" directly.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = DemoTokenService.CreateValidationParameters();
    });

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "SignalR authentication refresh demo. POST /token?user=alice, then connect to /clock.");
// DEMO ONLY, and Development only so a copy of this code can't expose these when deployed.
// /token issues a signed token for whatever user name is asked for, with no credential check,
// standing in for "the user has already signed in somewhere else". /promote grants that user the
// admin role, which shows up the next time they're issued a token. /reset undoes that so the demo
// can be run repeatedly against one server process. See DemoTokenService for what a
// real app should do instead.
if (app.Environment.IsDevelopment())
{
    app.MapPost("/token", (string user, UserDirectory users, DemoTokenService tokens) =>
        TypedResults.Ok(tokens.CreateToken(user, users.GetRoles(user))));

    app.MapPost("/promote", (string user, UserDirectory users) =>
    {
        users.Promote(user);

        return TypedResults.Ok($"{user} is now an admin. The new role appears in their next token.");
    });

    app.MapPost("/reset", (string user, UserDirectory users) =>
    {
        users.Reset(user);

        return TypedResults.Ok($"{user} is back to a standard user.");
    });
}

app.MapHub<ClockHub>("/clock", options =>
{
    // New in .NET 11. Lets a client swap in a newly issued access token on the live connection
    // instead of dropping it and reconnecting. The server has to opt in: negotiate only reports the
    // token's remaining lifetime when this is set, and without a reported lifetime the client never
    // schedules a refresh.
    options.EnableAuthenticationRefresh = true;

    // Close a connection once its token has expired. Off by default, and unchanged since .NET 6.
    // With refresh enabled a healthy client renews before this can fire; run the client's
    // "No refresh" profile to see the behavior this feature exists to avoid.
    options.CloseOnAuthenticationExpiration = true;

    // Optional policy gate, invoked after the new token is validated but before it is applied.
    // Returning false rejects the refresh with a 403 and leaves the connection on its current
    // principal. This is the place for policy SignalR can't know about, such as an absolute session
    // limit or a revocation check.
    //
    // It is also where an app re-asserts that a refresh may not change who the caller is. SignalR
    // rejects an identity change at the transport level for connections that span requests (long
    // polling, stateful reconnect), but enabling authentication refresh hands that decision to this
    // callback. The hub layer still aborts a connection whose user identifier changes, so nothing
    // gets silently re-bound either way. Failing here just fails better: it happens while the
    // refresh request is still in flight, so the client gets a 403 it can react to and keeps
    // running on its current token, instead of seeing the connection drop moments later.
    options.OnAuthenticationRefresh = context =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ClockHub>>();

        var previousSubject = context.PreviousUser.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var newSubject = context.NewUser.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (previousSubject is null || !string.Equals(previousSubject, newSubject, StringComparison.Ordinal))
        {
            logger.LogWarning("Rejected authentication refresh for {ConnectionId}: subject changed from {PreviousUser} to {NewUser}.",
                context.ConnectionId, previousSubject ?? "<none>", newSubject ?? "<none>");

            return ValueTask.FromResult(false);
        }

        logger.LogInformation("Accepted authentication refresh for {ConnectionId} as {User}; new expiration {NewExpiration:O}.",
            context.ConnectionId, newSubject, context.NewExpiration);

        return ValueTask.FromResult(true);
    };
});

app.Run();
