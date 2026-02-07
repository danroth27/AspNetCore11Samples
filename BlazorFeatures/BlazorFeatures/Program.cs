using BlazorFeatures.Client.Pages;
using BlazorFeatures.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
// =============================================================================
// FEATURE: SignalR ConfigureConnection
// The new ConfigureConnection option on ServerComponentsEndpointOptions allows
// configuring HttpConnectionDispatcherOptions for the SignalR connection.
// NOTE: The release notes code sample is INCORRECT. The actual API:
// - Is on AddInteractiveServerRenderMode, not AddBlazorHub
// - Takes Action<HttpConnectionDispatcherOptions>, not a HubConnection callback
// - Configures transport options, auth data, and connection-level settings
// =============================================================================
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(options =>
    {
        options.ConfigureConnection = connectionOptions =>
        {
            // Configure HTTP connection dispatcher options
            // Available options include:
            // - CloseOnAuthenticationExpiration
            // - Transports (WebSockets, LongPolling, ServerSentEvents)
            // - TransportMaxBufferSize
            // - ApplicationMaxBufferSize
            // - AuthorizationData
            // - MinimumProtocolVersion
            connectionOptions.CloseOnAuthenticationExpiration = true;
        };
    })
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorFeatures.Client._Imports).Assembly);

app.Run();
