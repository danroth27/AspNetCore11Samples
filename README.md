# ASP.NET Core .NET 11 Samples

Sample projects demonstrating new ASP.NET Core features across the .NET 11 preview
releases, built for the ASP.NET Community Standup livestream.

## Projects

### BlazorFeatures
Blazor Web App (Interactive Server/WebAssembly) demonstrating new Blazor components
and features. Demos are grouped by the preview that introduced them.

**Preview 1**
- **EnvironmentView** (`/environment-view`) — Conditional rendering based on hosting environment
- **Label Component** (`/label-demo`) — Accessible form labels with `[Display]` attribute support
- **DisplayName Component** (`/displayname-demo`) — Display property names from metadata attributes
- **QuickGrid OnRowClick** (`/quickgrid-onrowclick`) — Row click event handling
- **Navigation Features** (`/navigation-features/overview`) — `RelativeToCurrentUri` and `GetUriWithFragment()`
- **MathML Support** (`/mathml-demo`) — Proper MathML namespace in interactive rendering
- **InvokeVoidAsync() Analyzer** (`/invoke-void-async-analyzer`) — JSInterop best-practices analyzer

**Preview 2**
- **TempData for Blazor SSR** (`/tempdata-demo`) — `TempData` flash messages and multi-page flows
- **Label ID (Interactive)** (`/label-interactive-demo`) — `Label` generates matching `id`/`for` in interactive mode

**Preview 3**
- **Variable-Height Virtualize** (`/virtualize-demo`) — `Virtualize` with variable-height items

**Preview 4**
- **SupplyParameterFromTempData** (`/tempdata`) — `[SupplyParameterFromTempData]` one-shot flash messages
- **Virtualize AnchorMode** (`/virtualize-anchor`) — `AnchorMode` + `ItemComparer` keep the viewport stable
- **Circuit Pause** (`/circuit-pause`) — Server-initiated circuit pause/resume

**Preview 5**
- **Client-side Validation** (`/client-validation`) — Validation that runs on the client
- **Async Validation** (`/async-validation`) — Async form validation with localized messages. Updated in Preview 6 to use async DataAnnotations (`AsyncValidationAttribute`), the same hooks `ApiFeatures` uses for minimal APIs
- **QuickGrid SSR** (`/quickgrid-ssr`) — QuickGrid in statically rendered pages
- **Session Parameter** (`/session-parameter`) — `[SupplyParameterFromSession]`

**Preview 6**
- **C# Unions in Blazor** (`/unions-demo`) — `union ToastMessage(string, RenderFragment)`
  as a single "text or template" component parameter, plus `DynamicComponent` with a
  boxed-union parameter. Requires `<LangVersion>preview</LangVersion>` and
  `<EnablePreviewFeatures>true</EnablePreviewFeatures>`.
  The union deliberately omits a `MarkupString` (raw HTML) case to avoid an XSS footgun.
  Preview 7 fixed the literal-attribute shortcut for union-typed parameters, so
  `Content="hello"` now compiles ([dotnet/razor#13188](https://github.com/dotnet/razor/issues/13188)).
  One Razor limitation remains: child-content markup doesn't populate a `RenderFragment` case
  ([dotnet/razor#13200](https://github.com/dotnet/razor/issues/13200)).
  See the design note: [aspnet/specs#782](https://github.com/aspnet/specs/pull/782).
  The `Toast` component and `ToastMessage` union live in the shared `SharedComponents`
  Razor Class Library (referenced by both the Server and WebAssembly apps).
- **Virtualize scroll-to-item** (`/virtualize-scroll`) — `Virtualize<TItem>.InitialItemIndex`
  opens a large list at a given item, and `ScrollToItemAsync` scrolls to any item on demand
  ([dotnet/aspnetcore#66753](https://github.com/dotnet/aspnetcore/pull/66753)).
  These shipped in Preview 6 as `InitialIndex` and `ScrollToIndexAsync`; Preview 7 renamed both
  ([dotnet/aspnetcore#67914](https://github.com/dotnet/aspnetcore/pull/67914)).
- **Configure client from server** (`/browser-options`) — `WithBrowserOptions` sets client-side
  startup behavior (log level, reconnection, DOM preservation) from the server in C# instead of
  hand-written `Blazor.start` JavaScript; the page reads the resolved options with
  `HttpContext.GetBrowserOptions()` ([dotnet/aspnetcore#67337](https://github.com/dotnet/aspnetcore/pull/67337)).
- **Automatic CSRF protection** (`/csrf-protection`) — an SSR form protected automatically by the
  new cross-origin checks (`Sec-Fetch-Site`/`Origin`) with no antiforgery token and no
  `app.UseAntiforgery()` ([dotnet/aspnetcore#66585](https://github.com/dotnet/aspnetcore/pull/66585)).
  The separate `CsrfAttackerSite` project forges a cross-site POST against this form.
**Preview 7**
- **Cache SSR output with `CacheView`** (`/cache-view`) — caches the rendered HTML of a
  statically rendered subtree with `ExpiresAfter` and vary-by dimensions. On a cache hit the
  children are not instantiated at all. Also shows the `[CacheBehavior(CacheBehavior.Rerender)]`
  "hole" that keeps updating inside a cached region, and the `[CacheCondition]` guard that forces
  `QuickGrid` to be paired with `VaryByQuery`. `Program.cs` registers `AddHybridCache()`, which
  `CacheView` picks up from DI automatically
  ([dotnet/aspnetcore#65772](https://github.com/dotnet/aspnetcore/pull/65772),
  [#67776](https://github.com/dotnet/aspnetcore/pull/67776)).
- **QuickGrid scroll-to-item** (`/quickgrid-scroll`) — `QuickGrid` forwards
  `InitialItemIndex` and `ScrollToItemAsync` to its inner `Virtualize`, so a virtualized grid can
  open at a specific row and be scrolled programmatically. In the Preview 7 build, refresh the page
  after enhanced navigation to trigger a full page load before verifying `InitialItemIndex`
  ([dotnet/aspnetcore#67914](https://github.com/dotnet/aspnetcore/pull/67914)).
- **Automatic circuit pause** (`/auto-pause`) — the circuit pauses itself after the tab
  has been hidden for `HiddenDelay`, releasing the SignalR connection and server memory. Configured
  with `options.AddAutoPause(...)` from the `Microsoft.AspNetCore.Components.Server.AutoPause`
  package. Its counter uses `[PersistentState]` because ordinary component
  fields are not automatically serialized across pause/resume. The circuit ID and a non-persisted
  component-instance ID both change when the circuit is resumed and reconstructed, while the persisted
  counter keeps its value, making pause/resume observable without app-level JavaScript. With the Preview 7
  build, open or refresh `/auto-pause` directly before testing. Enhanced navigation from an initially
  non-interactive page doesn't invoke the package's startup callback
  ([dotnet/aspnetcore#68337](https://github.com/dotnet/aspnetcore/issues/68337)).
  ([dotnet/aspnetcore#67098](https://github.com/dotnet/aspnetcore/pull/67098),
  [#67045](https://github.com/dotnet/aspnetcore/pull/67045)).
- **New Blazor analyzers** (`/analyzers`) — `AnalyzerDemo.razor` deliberately violates
  `BL0012`–`BL0016`, so building the project reports all five new diagnostics.

### BlazorFeatures.E2E.Tests
End-to-end tests for the BlazorFeatures app using the new
`Microsoft.AspNetCore.Components.Testing` library (Preview 4), which combines xUnit v3
collection fixtures, a YARP reverse proxy, and Playwright with Blazor-aware wait helpers.
See [BlazorFeatures.E2E.Tests/README.md](BlazorFeatures.E2E.Tests/README.md) for details.

### BlazorWasmFeatures
Standalone Blazor WebAssembly app demonstrating WASM-specific features:

- **IHostedService Support** — Background services running in the browser
- **Environment Variables** — Access environment variables via `IConfiguration`
- **Web Worker** (`/web-worker`) — Offload CPU-intensive work to a Web Worker running a
  separate .NET runtime (uses the `WebWorkerDemo` library)
- **C# Unions (Preview 6)** (`/unions-demo`) — Verified to work end-to-end in a published,
  trimmed WASM build (default ILLink trimming): `ToastMessage` rendering of the `string` and
  `RenderFragment` cases.
- **Gateway backend proxy (Preview 6)** (`/weather`) — The dev-time Blazor Gateway
  (`Microsoft.AspNetCore.Components.Gateway`) proxies the client's `api/weather` calls to the
  separate `BackendApi` service via YARP. Because the WASM client only ever makes same-origin
  requests to the gateway, **no CORS configuration is required** on the client or the backend.
  The proxy route is supplied to the gateway through the `ReverseProxy` config keys in
  `Properties/launchSettings.json`. Requires the gateway to run both the app and the backend
  (see [Running the Samples](#running-the-samples)).

### WebWorkerDemo
Reusable Razor class library that wires up a `[JSExport]`/`[JSImport]` Web Worker host so
Blazor WebAssembly apps can run .NET work off the UI thread. Consumed by `BlazorWasmFeatures`.

### SharedComponents
Generic Razor Class Library for components and types shared across the sample apps. Currently
hosts the `Toast` component and the `ToastMessage` C# union, consumed by both `BlazorFeatures`
(Server) and `BlazorWasmFeatures` (WebAssembly).

### ApiFeatures
Web API (minimal APIs) demonstrating framework features:

- **OpenAPI 3.2** (Preview 2) — `OpenApiVersion = OpenApi3_2`
- **Validation source generator** (Preview 2) — Handles `JsonElement`/`Dictionary` indexer properties
- **Native OpenTelemetry tracing** (Preview 2) — Framework emits HTTP semantic-convention tags by default
- **Zstandard response compression** (Preview 3) — zstd as a default provider (`zstd > br > gzip`)
- **HTTP QUERY method** (Preview 3) — GET-like requests with a body for complex searches (`QUERY /search`)
- **FileContentResult/FileStreamResult in OpenAPI** (Preview 4) — Documented as `{ type: string, format: binary }`
- **Endpoint filters observe binding failures** (Preview 4) — Filter pipeline runs even when parameter binding fails
- **Enum parameter naming in OpenAPI** (Preview 5) — Non-body enum params keep their C# names; array schema IDs use valid names
- **Kestrel trailer header timeout** (Preview 5) — `RequestHeadersTimeout` applies to HTTP/2 and HTTP/3 trailer frames
- **C# unions** (Preview 6) — union return types are described with `anyOf` in OpenAPI and serialized by their active case (`GET /pets/{id}`); a union body binds by JSON token type (`POST /pets/adopt`)
- **Async validation** (Preview 6) — `AsyncValidationAttribute` (`POST /register`) and `IAsyncValidatableObject` (`POST /reservations`) run during minimal-API validation via `AddValidation()`
- **TLS channel binding token access** (Preview 7, [dotnet/aspnetcore#67436](https://github.com/dotnet/aspnetcore/pull/67436), follow-up [#67720](https://github.com/dotnet/aspnetcore/pull/67720)) — `GET /channel-binding` reads the RFC 5929 `tls-server-end-point` token from `ITlsConnectionFeature.TryGetChannelBindingBytes(ChannelBindingKind.Endpoint, out ...)`. Channel binding ties an authentication exchange to the specific TLS channel, defeating auth-relay / MITM attacks that replay credentials onto another connection. Kestrel implements it over `SslStream.TransportContext.GetChannelBinding`, so it works on any HTTPS connection — **run with the `https` profile** (`dotnet run --project ApiFeatures --launch-profile https`) and call `https://localhost:7123/channel-binding`. The endpoint returns a SHA-256 fingerprint of the token (the token derives from the public server certificate, so it isn't secret) rather than the raw bytes. On HTTP.sys the new `HttpSysOptions.HttpAuthenticationHardeningLevel` (Legacy/Medium/Strict, default Medium) additionally lets the OS enforce channel binding on Windows auth, and Strict fails startup if that hardening can't be applied (#67720).
- **Server-sent events in OpenAPI** (Preview 7, [dotnet/aspnetcore#67461](https://github.com/dotnet/aspnetcore/pull/67461)) — `GET /todos/stream`, `/todos/stream-simple`, and `/ticks/stream` return `TypedResults.ServerSentEvents(...)`, and the generated document describes them with OpenAPI 3.2 `itemSchema` under `text/event-stream`. `ServerSentEvents.cs` documents the two overload pitfalls: returning a bare `IAsyncEnumerable<SseItem<T>>` from the lambda produces `application/json` instead of SSE, and passing `eventType:` alongside `SseItem<T>` values binds the wrong overload and double-wraps the payload.
- **Security hardening** (Preview 7) — `SecurityHardening.cs` exercises four Preview 7 changes as raw HTTP behavior: Rewrite middleware collapsing leading `/` and `\` runs so a rule can't emit a scheme-relative open redirect (`/open-redirect-slashes`, [#66961](https://github.com/dotnet/aspnetcore/pull/66961), [#67928](https://github.com/dotnet/aspnetcore/pull/67928)); `PathString.StartsWithSegments` treating `\` as a segment boundary so `/segment-guard%5Cbar` no longer bypasses a `Map` branch ([#67093](https://github.com/dotnet/aspnetcore/pull/67093)); Kestrel rejecting `Content-Length: +5` ([#67635](https://github.com/dotnet/aspnetcore/pull/67635)); and the default CSRF middleware validating only endpoints with antiforgery metadata, so `POST /csrf/plain` passes cross-origin while `POST /csrf/form` is still rejected ([#67460](https://github.com/dotnet/aspnetcore/pull/67460), [#67839](https://github.com/dotnet/aspnetcore/pull/67839)).


### SignalRFeatures and SignalRClient
SignalR authentication-refresh demo for .NET 11 Preview 6:

- **Authentication refresh** (Preview 6, [dotnet/aspnetcore#67400](https://github.com/dotnet/aspnetcore/pull/67400)) — the server enables `EnableAuthenticationRefresh` on `/clock`, and the .NET client refreshes its bearer token without dropping the hub connection. Tokens last 45 seconds so an automatic refresh is visible during a short run.
- `SignalRFeatures` hosts the JWT bearer-secured `/clock` hub, the `/token?user=alice` issuer, and `/promote?user=alice` on `https://localhost:7110`. The client also calls `/reset?user=alice` at startup, so the demo can be re-run repeatedly against one server process.
- `SignalRClient` streams clock ticks for 50 seconds and prints auth-refresh callbacks plus a success summary. Eight seconds in it promotes `alice` to `admin` and calls `RefreshAuthenticationAsync()`, so the new role shows up on the next tick over the same connection.

**Security notes (this sample deliberately simplifies auth; don't copy these into production):**

- The `/token` endpoint issues a signed JWT for any requested username **with no credential check**, and `/promote` and `/reset` change the admin role for anyone who asks — all three stand in for real sign-in and real entitlement management. They are registered **only in Development** so a copy of this code can't expose them when deployed. Real apps authenticate the user (ASP.NET Core Identity, Microsoft Entra ID, or another IdP) and issue tokens from that trusted source; validate them with `JwtBearerOptions.Authority` instead of a local key.
- The signing key is **generated per process** with `RandomNumberGenerator.GetBytes(32)`, so **no key material is checked into this repo** — the same approach used by the SignalR [`JwtSample`](https://github.com/dotnet/aspnetcore/blob/main/src/SignalR/samples/JwtSample/Startup.cs) in `dotnet/aspnetcore`. Restarting the server invalidates tokens from the previous run.
- **Enabling `EnableAuthenticationRefresh` hands principal-change policy to the app.** SignalR normally rejects a request whose user differs from the one a connection is bound to; for connections that span requests (long polling, stateful reconnect) that transport-level check is skipped once refresh is enabled, because `OnAuthenticationRefresh` is now the place to make that call. The hub layer still aborts a connection whose user identifier changes, so an identity swap never silently succeeds — but that abort happens *after* the refresh request has already returned success. This sample's `OnAuthenticationRefresh` compares the `sub` claim of `PreviousUser` and `NewUser` and rejects the refresh with `403` while it's still in flight, so the client gets an actionable error and keeps running on its current token.
- What the sample does follow: `[Authorize]` on the hub, HTTPS, full token validation (issuer/audience/key/lifetime), an identity check on refresh, and `CloseOnAuthenticationExpiration` so a connection that isn't refreshed is closed at token expiry.

### BackendApi
Minimal Web API that serves weather data at `/api/weather`. It is the backend service that
`BlazorWasmFeatures` calls through the Blazor Gateway's YARP proxy. It has **no CORS
configuration on purpose** — the browser only talks to the gateway (same origin), and the
gateway-to-backend hop happens server-side.

### CsrfAttackerSite
A deliberately separate origin for the automatic CSRF demo. It serves a single static page
(a fake "you won a prize" site) whose hidden form posts a funds transfer to `BlazorFeatures`
at `http://localhost:5059/csrf-protection`. Browse to it at **`http://127.0.0.1:8080`** —
`127.0.0.1` is a different *site* from the bank's `localhost`, so the browser marks the request
`Sec-Fetch-Site: cross-site` and .NET 11's automatic CSRF protection rejects it with `400`.
See [Automatic CSRF protection demo](#automatic-csrf-protection-demo) for how to run it.

## Running the Samples

Requires the .NET 11 Preview 7 SDK (`11.0.100-preview.7.26381.103`) or later.

```pwsh
dotnet build

# Run a project, e.g. the Blazor Web App:
dotnet run --project BlazorFeatures
```


### Automatic CSRF protection demo

Run the Blazor app and the attacker site in separate terminals, then browse to the attacker at
**`http://127.0.0.1:8080`** (note `127.0.0.1`, not `localhost`, so it's a different site):

```pwsh
# Terminal 1 — the "bank" app on http://localhost:5059
dotnet run --project BlazorFeatures --launch-profile http

# Terminal 2 — the attacker site on http://127.0.0.1:8080
dotnet run --project CsrfAttackerSite --launch-profile http
```

Submitting the transfer form on `/csrf-protection` directly (same-origin) succeeds. Clicking
"Claim your prize" on the attacker page posts the same form from a different site; open the
browser dev tools **Network** tab (enable "Preserve log") to see the request carry
`Sec-Fetch-Site: cross-site` and get rejected with **400**.

### SignalR authentication refresh

Start the server, then run the client. In Visual Studio, right-click `SignalRFeatures` → **Debug** →
**Start Without Debugging**, then set `SignalRClient` as the startup project and Ctrl+F5. From the
command line:

```pwsh
# Terminal 1 — SignalR server on https://localhost:7110
dotnet run --project SignalRFeatures

# Terminal 2 — ticks continue across a role change and a token expiry
dotnet run --project SignalRClient
```

The client shows `roles=user` for the first few ticks. At 8 seconds it promotes `alice` and calls
`RefreshAuthenticationAsync()`, so the next tick reads `roles=user,admin` on the same connection id.
Around 40 seconds the automatic refresh fires and `token-exp` moves forward.

To show the previous behavior for contrast, disable client auto-refresh. Because the hub also sets `CloseOnAuthenticationExpiration`, the connection closes when the 45-second token expires:

```pwsh
dotnet run --project SignalRClient -- --no-refresh --duration-seconds 60
```

To show the identity check, have the client request its *refresh* token for a different user. The server's `OnAuthenticationRefresh` compares the `sub` claim and rejects the refresh with `403`, so the connection is never re-bound to `bob`:

```pwsh
dotnet run --project SignalRClient -- --refresh-as bob --duration-seconds 55
```

### Blazor Gateway backend proxy (no CORS)

To try the standalone WASM app calling a backend through the gateway proxy, run the backend
and the WASM app (hosted by the gateway) together, then browse to `/weather`:

```pwsh
# Terminal 1 — backend service on http://localhost:5100
dotnet run --project BackendApi --launch-profile http

# Terminal 2 — WASM app hosted by the Blazor Gateway on http://localhost:5090
dotnet run --project BlazorWasmFeatures --launch-profile http
```

Open http://localhost:5090/weather. The client fetches `api/weather` from the gateway origin,
and the gateway proxies it to `BackendApi` via YARP. The proxy route/cluster (and the backend
address) are configured with `ReverseProxy__*` environment variables in
`BlazorWasmFeatures/Properties/launchSettings.json`.

Several Preview 7 packages are not on nuget.org yet, so `NuGet.config` adds the
[dotnet11 daily-build feed](https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet11/nuget/v3/index.json).
`Microsoft.AspNetCore.Components.QuickGrid`,
`Microsoft.AspNetCore.Components.Server.AutoPause`, and
`Microsoft.AspNetCore.Components.Gateway` all come from that feed at
`11.0.0-preview.7.26381.103`.
