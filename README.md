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
  Current Razor limitations for union-typed parameters: the literal-attribute shortcut
  (`Content="hello"`) doesn't compile — use the expression form `Content="@("hello")"`
  ([dotnet/razor#13188](https://github.com/dotnet/razor/issues/13188)) — and child-content
  markup doesn't populate a `RenderFragment` case
  ([dotnet/razor#13200](https://github.com/dotnet/razor/issues/13200)).
  See the design note: [aspnet/specs#782](https://github.com/aspnet/specs/pull/782).
  The `Toast` component and `ToastMessage` union live in the shared `SharedComponents`
  Razor Class Library (referenced by both the Server and WebAssembly apps).
- **Virtualize scroll-to-item** (`/virtualize-scroll`) — `Virtualize<TItem>.InitialIndex`
  opens a large list at a given item, and `ScrollToIndexAsync` scrolls to any item on demand
  ([dotnet/aspnetcore#66753](https://github.com/dotnet/aspnetcore/pull/66753)).
- **Configure client from server** (`/browser-options`) — `WithBrowserOptions` sets client-side
  startup behavior (log level, reconnection, DOM preservation) from the server in C# instead of
  hand-written `Blazor.start` JavaScript; the page reads the resolved options with
  `HttpContext.GetBrowserOptions()` ([dotnet/aspnetcore#67337](https://github.com/dotnet/aspnetcore/pull/67337)).
- **Automatic CSRF protection** (`/csrf-protection`) — an SSR form protected automatically by the
  new cross-origin checks (`Sec-Fetch-Site`/`Origin`) with no antiforgery token and no
  `app.UseAntiforgery()` ([dotnet/aspnetcore#66585](https://github.com/dotnet/aspnetcore/pull/66585)).
  The separate `CsrfAttackerSite` project forges a cross-site POST against this form.

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


### SignalRFeatures and SignalRClient
SignalR authentication-refresh demo for .NET 11 Preview 6:

- **Authentication refresh** (Preview 6, [dotnet/aspnetcore#67400](https://github.com/dotnet/aspnetcore/pull/67400)) — the server enables `EnableAuthenticationRefresh` on `/clock`, and the .NET client refreshes its bearer token before expiry without dropping the hub connection. Tokens last 45 seconds so refresh is visible during a short run.
- `SignalRFeatures` hosts the JWT bearer-secured `/clock` hub and `/token?user=alice` issuer on `http://localhost:5110`.
- `SignalRClient` streams clock ticks for 75 seconds and prints auth-refresh callbacks plus a success summary.

**Security notes (this sample deliberately simplifies auth; don't copy these into production):**

- The `/token` endpoint issues a signed JWT for any requested username **with no credential check** — it stands in for a real sign-in. It is registered **only in Development** so a copy of this code can't expose it when deployed. Real apps authenticate the user (ASP.NET Core Identity, Microsoft Entra ID, or another IdP) and issue tokens from that trusted source; validate them with `JwtBearerOptions.Authority` instead of a local key.
- The signing key is **generated per process** with `RandomNumberGenerator.GetBytes(32)`, so **no key material is checked into this repo** — the same approach used by the SignalR [`JwtSample`](https://github.com/dotnet/aspnetcore/blob/main/src/SignalR/samples/JwtSample/Startup.cs) in `dotnet/aspnetcore`. Restarting the server invalidates tokens from the previous run.
- The sample runs over plain **HTTP** on localhost. Production must use **HTTPS**. Browser clients on WebSockets/SSE can't set an `Authorization` header and send the token in the query string, which is commonly logged — see [SignalR security: access token logging](https://learn.microsoft.com/aspnet/core/signalr/security#access-token-logging). (The .NET client used here sends an `Authorization` header instead.)
- **Enabling `EnableAuthenticationRefresh` opts a connection out of SignalR's built-in "reject if the user changed" hardening** — the app owns that policy. This sample's `OnAuthenticationRefresh` compares the `sub` claim of `PreviousUser` and `NewUser` and rejects any refresh that would re-bind a live connection to a different identity.
- What the sample does follow: `[Authorize]` on the hub, full token validation (issuer/audience/key/lifetime), the query-string token restricted to the hub path, an identity check on refresh, and `CloseOnAuthenticationExpiration` so a connection that isn't refreshed is closed at token expiry.

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

Requires the .NET 11 Preview 6 SDK (`11.0.100-preview.6.26359.118`) or later.

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

Run the server and client in separate terminals. The client output should show ticks continuing across at least one 45-second token expiry and an `AUTH REFRESHED` line without any reconnect or close.

```pwsh
# Terminal 1 — SignalR server on http://localhost:5110
dotnet run --project SignalRFeatures --launch-profile http

# Terminal 2 — .NET SignalR client for 75 seconds
dotnet run --project SignalRClient -- --server http://localhost:5110 --user alice --duration-seconds 75
```

To show the previous behavior for contrast, disable client auto-refresh. Because the hub also sets `CloseOnAuthenticationExpiration`, the connection closes when the 45-second token expires:

```pwsh
dotnet run --project SignalRClient -- --server http://localhost:5110 --user alice --duration-seconds 55 --no-refresh
```

To show the identity check, have the client request its *refresh* token for a different user. The server's `OnAuthenticationRefresh` compares the `sub` claim and rejects the refresh with `403`, so the connection is never re-bound to `bob`:

```pwsh
dotnet run --project SignalRClient -- --server http://localhost:5110 --user alice --refresh-as bob --duration-seconds 55
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

All packages are published on nuget.org, so no extra NuGet feeds are required.
