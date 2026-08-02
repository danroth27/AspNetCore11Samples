using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.Features;

namespace ApiFeatures;

// Preview 7 (#67436, follow-up #67720): ITlsConnectionFeature.TryGetChannelBindingBytes
// exposes the RFC 5929 TLS channel binding token to the app, portably across Kestrel,
// HTTP.sys, and IIS. Channel binding ties an authentication exchange to the specific TLS
// channel it happened on, which defeats authentication-relay / man-in-the-middle attacks
// that terminate the client's TLS and replay its credentials onto a different connection.
//
// Kestrel implements this over SslStream.TransportContext.GetChannelBinding(kind), so the
// token is available on any HTTPS connection — no Windows auth is required to observe it
// here. On HTTP.sys the new HttpAuthenticationHardeningLevel option (Legacy/Medium/Strict,
// defaulting to Medium) additionally lets the OS enforce channel binding on Windows auth,
// and Strict fails startup if that hardening can't be applied (#67720).
public static class ChannelBinding
{
    public static void MapChannelBinding(this IEndpointRouteBuilder app)
    {
        app.MapGet("/channel-binding", (HttpContext context) =>
        {
            var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
            if (tlsFeature is null)
            {
                return Results.Ok(new ChannelBindingResult(
                    Https: false,
                    ChannelBindingAvailable: false,
                    Message: "No TLS connection feature — this request did not arrive over HTTPS. " +
                             "Call this endpoint over https:// (profile 'https', port 7123) to see a token."));
            }

            // tls-server-end-point (RFC 5929): a hash of the server's TLS certificate. Unlike
            // tls-unique it is available on both TLS 1.2 and TLS 1.3, so it is the practical
            // choice for a demo that runs on a modern stack.
            if (tlsFeature.TryGetChannelBindingBytes(ChannelBindingKind.Endpoint, out var token))
            {
                var bytes = token.ToArray();

                // The token is derived from the *public* server certificate, so it is not secret.
                // We surface a SHA-256 fingerprint (not the raw bytes) to keep the output compact
                // and to make it obvious that the value is stable across requests to this endpoint.
                return Results.Ok(new ChannelBindingResult(
                    Https: true,
                    ChannelBindingAvailable: true,
                    Kind: "tls-server-end-point",
                    LengthBytes: bytes.Length,
                    TokenSha256: Convert.ToHexString(SHA256.HashData(bytes))));
            }

            return Results.Ok(new ChannelBindingResult(
                Https: true,
                ChannelBindingAvailable: false,
                Message: "The TLS connection feature is present but no channel binding token was " +
                         "available for ChannelBindingKind.Endpoint on this connection."));
        })
        .WithName("GetChannelBinding")
        .WithDescription("Preview 7 (#67436): reads the RFC 5929 TLS channel binding token via ITlsConnectionFeature.TryGetChannelBindingBytes");
    }
}

record ChannelBindingResult(
    bool Https,
    bool ChannelBindingAvailable,
    string? Kind = null,
    int? LengthBytes = null,
    string? TokenSha256 = null,
    string? Message = null);
