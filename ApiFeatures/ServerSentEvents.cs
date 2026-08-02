using System.Runtime.CompilerServices;
using System.Net.ServerSentEvents;

namespace ApiFeatures;

// Preview 7 (#67461): endpoints that stream server-sent events are now described in the
// generated OpenAPI document with the OpenAPI 3.2 `itemSchema` keyword, so the *payload* of
// each event has a real schema instead of collapsing to a plain `string`.
//
// This builds on Preview 6, which made OpenAPI 3.2 the default output version. `itemSchema`
// is a 3.2 addition for streaming media types; a 3.0/3.1 document has nowhere to put a
// per-event schema.
//
// Two things are easy to get wrong here, both verified against the generated document:
//
//  1. Returning IAsyncEnumerable<SseItem<T>> *directly* from the lambda does NOT produce SSE.
//     Minimal APIs serialize it as a JSON array (Content-Type: application/json), and the
//     OpenAPI response is documented that way too. You have to go through
//     TypedResults.ServerSentEvents(...) to get text/event-stream and `itemSchema`.
//
//  2. There are two relevant overloads:
//         ServerSentEvents<T>(IAsyncEnumerable<T>, string? eventType)
//         ServerSentEvents<T>(IAsyncEnumerable<SseItem<T>>)          // no eventType parameter
//     Passing `eventType:` together with an IAsyncEnumerable<SseItem<Todo>> binds the *first*
//     overload with T = SseItem<Todo>, which serializes the whole envelope into `data`
//     ({"data":{...},"eventType":...,"eventId":...}) and documents `data` as `SseItemOfTodo`.
//     Use the SseItem overload with no eventType when you set per-event ids yourself.
//
// Inspect the result while the app is running: GET /openapi/v1.json
public static class ServerSentEvents
{
    public static void MapServerSentEvents(this IEndpointRouteBuilder app)
    {
        // Per-event control: SseItem<T> carries the payload plus the optional SSE `event`
        // and `id` fields. OpenAPI documents `data` as $ref: '#/components/schemas/Todo'.
        app.MapGet("/todos/stream", (CancellationToken cancellationToken) =>
            TypedResults.ServerSentEvents(StreamTodosAsync(cancellationToken)))
            .WithName("StreamTodos")
            .WithDescription("Preview 7 (#67461): SseItem<Todo> stream described with OpenAPI 3.2 itemSchema");

        // Simplest form: stream the payload type directly and set one event name for the
        // whole stream. Produces the same itemSchema shape.
        app.MapGet("/todos/stream-simple", (CancellationToken cancellationToken) =>
            TypedResults.ServerSentEvents(StreamPlainTodosAsync(cancellationToken), eventType: "todo"))
            .WithName("StreamTodosSimple")
            .WithDescription("Preview 7 (#67461): IAsyncEnumerable<Todo> stream with a fixed event type");

        // A stream of a primitive type still gets a typed itemSchema (`format: int32`),
        // which is the part that used to be lost.
        app.MapGet("/ticks/stream", (CancellationToken cancellationToken) =>
            TypedResults.ServerSentEvents(StreamTicksAsync(cancellationToken)))
            .WithName("StreamTicks")
            .WithDescription("Preview 7 (#67461): SseItem<int> stream — itemSchema data is an integer");
    }

    private static async IAsyncEnumerable<SseItem<Todo>> StreamTodosAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var todos = new[]
        {
            new Todo(1, "Read the Preview 7 release notes", true),
            new Todo(2, "Try SSE in OpenAPI 3.2", false),
            new Todo(3, "Ship something", false),
        };

        foreach (var todo in todos)
        {
            yield return new SseItem<Todo>(todo) { EventId = todo.Id.ToString() };
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }

    private static async IAsyncEnumerable<Todo> StreamPlainTodosAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var todo in new[] { new Todo(1, "First", false), new Todo(2, "Second", true) })
        {
            yield return todo;
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }

    private static async IAsyncEnumerable<SseItem<int>> StreamTicksAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 1; i <= 5; i++)
        {
            yield return new SseItem<int>(i);
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }
}
