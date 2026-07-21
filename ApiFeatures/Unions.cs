// C# unions in ASP.NET Core (dotnet/aspnetcore #66951).
//
// C# union types are a preview language feature in .NET 11, and System.Text.Json
// serializes them natively. Because ASP.NET Core uses System.Text.Json for JSON,
// unions work as JSON request bodies and return types across Minimal APIs, MVC,
// SignalR, and Blazor with no ASP.NET-specific configuration.
//
// For OpenAPI, an endpoint that returns a union is described with an `anyOf` schema
// listing each case type (dotnet/aspnetcore #67001). Union cases don't carry a
// `$type` discriminator, so a case like `Dog` reuses the standalone
// `#/components/schemas/Dog` component instead of a duplicated, prefixed one.

namespace ApiFeatures;

public record class Dog(string Name, bool GoodBoy = true);
public record class Cat(int Lives);

// A single type that is "a Dog or a Cat" — no discriminator property required.
// Used as a RETURN type: STJ writes the active case and OpenAPI describes it as
// anyOf: [Dog, Cat].
public union Pet(Dog, Cat);

// A union used as a REQUEST BODY. The cases have different JSON token types
// (a string vs. an object), so System.Text.Json deserializes them unambiguously
// with no custom classifier — a realistic "reference an existing pet by name OR
// send a full new pet" shape. (Object-only unions like Pet above share the JSON
// Object token, so as a body they'd need a custom JsonTypeClassifier to disambiguate.)
public union PetInput(string, Dog);

public static class UnionEndpoints
{
    public static IEndpointRouteBuilder MapUnions(this IEndpointRouteBuilder app)
    {
        // Union RETURN type. The active case is serialized on the way out and the
        // endpoint is described with `anyOf: [Dog, Cat]` in the OpenAPI document.
        app.MapGet("/pets/{id}", Pet (int id) => id == 0 ? new Dog("Rex") : new Cat(9))
            .WithName("GetPet")
            .WithDescription("Returns a union (Dog|Cat) — OpenAPI describes it with anyOf; STJ serializes the active case.");

        // Union BODY parameter. Post either a JSON string ("Rex") to reference an
        // existing pet, or a JSON object ({ "name": "Fido" }) to send a new one.
        // STJ picks the case by JSON token type.
        app.MapPost("/pets/adopt", (PetInput input) =>
        {
            var message = input.Value switch
            {
                string name => $"Adopting existing pet '{name}'.",
                Dog dog => $"Registering and adopting new dog '{dog.Name}'.",
                _ => "Unknown input.",
            };
            return TypedResults.Ok(new { message });
        })
        .WithName("AdoptPet")
        .WithDescription("Accepts a union body (string id | Dog object); STJ binds by JSON token type.");

        return app;
    }
}
