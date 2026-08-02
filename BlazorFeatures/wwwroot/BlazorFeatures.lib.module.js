// Blazor JS initializer for BlazorFeatures.
//
// Preview 7 (#67098, #67045) lets an app defer a circuit pause while it finishes
// work that would otherwise be lost. The extensibility point is a *circuit handler*
// with an `onCircuitPausing(signal)` callback: Blazor awaits every registered
// handler before it actually pauses, for both auto-pause (tab hidden) and
// server-initiated `Circuit.RequestCircuitPauseAsync` pauses.
//
// `signal` is an AbortSignal that fires if the pause is cancelled (for example
// because the tab became visible again), so long-running work can bail out.

export function beforeWebStart(options) {
  options.circuit ??= {};
  options.circuit.circuitHandlers ??= [];

  options.circuit.circuitHandlers.push({
    onCircuitPausing: async (signal) => {
      console.log('[auto-pause] circuit is about to pause; flushing pending work...');

      // Stand-in for real work: persist drafts, flush analytics, close a transaction.
      await new Promise((resolve) => {
        const id = setTimeout(resolve, 500);
        signal?.addEventListener('abort', () => {
          clearTimeout(id);
          console.log('[auto-pause] pause was cancelled; work aborted.');
          resolve();
        }, { once: true });
      });

      console.log('[auto-pause] done; circuit may now pause.');
    },

    onCircuitOpened: () => console.log('[circuit] opened'),
    onCircuitClosed: () => console.log('[circuit] closed'),
  });
}
