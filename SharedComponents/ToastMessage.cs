using Microsoft.AspNetCore.Components;

namespace SharedComponents;

// .NET 11 Preview 6 — a component-library parameter that is text OR a template,
// expressed as a single C# union instead of the usual string + RenderFragment
// parameter pair.
//
// No MarkupString (raw HTML) case on purpose: exposing raw HTML through a public
// component parameter is an XSS footgun — string is HTML-encoded and
// RenderFragment is safely composed.
public union ToastMessage(string, RenderFragment);

public enum ToastSeverity { Info, Success, Warning, Error }
