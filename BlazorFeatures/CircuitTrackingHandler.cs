using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace BlazorFeatures;

// Lets components reach the active Circuit so they can call
// Circuit.RequestCircuitPauseAsync (Preview 4 #66265 / #66455).
//
// CircuitHandler.OnConnectionUpAsync is the supported way to acquire a
// Circuit reference from server-side code; there is no public registry.
public class CircuitTrackingHandler : CircuitHandler
{
    private static readonly ConcurrentDictionary<string, Circuit> _circuits = new();

    public Circuit? CurrentCircuit { get; private set; }

    public static IReadOnlyCollection<Circuit> ActiveCircuits => _circuits.Values.ToArray();

    public static Circuit? GetCircuit(string id)
        => _circuits.TryGetValue(id, out var c) ? c : null;

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        CurrentCircuit = circuit;
        _circuits[circuit.Id] = circuit;
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        CurrentCircuit = null;
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _circuits.TryRemove(circuit.Id, out _);
        return Task.CompletedTask;
    }
}
