// A deliberately separate "attacker" origin for the automatic CSRF demo. It only
// serves a static page (wwwroot/index.html) whose form posts to the BlazorFeatures
// app at http://localhost:5059/csrf-protection. Because that request comes from a
// different origin, the browser adds Sec-Fetch-Site: cross-site, and .NET 11's
// automatic CSRF protection rejects it with 400 before the handler runs.
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.Run();
