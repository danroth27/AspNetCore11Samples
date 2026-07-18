namespace BlazorFeatures.Models;

// .NET 11 Preview 6 — C# unions in Blazor.
//
// Component library API design: every Blazor component library (MudBlazor,
// FluentUI, Radzen, …) ships label/title/header parameters as TWO parameters
// today — a string and a RenderFragment — because there was no way to accept
// "text OR a template" as a single parameter. With a union, there is.
//
// Note: we deliberately do NOT include a MarkupString (raw HTML) case. A union
// makes it trivial to add one, but exposing raw HTML through a public component
// parameter is an XSS footgun — prefer string (HTML-encoded) and RenderFragment
// (safely composed).
public union ToastMessage(string, Microsoft.AspNetCore.Components.RenderFragment);
