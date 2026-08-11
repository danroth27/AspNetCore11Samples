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

let circuitBusy = false;
let hiddenDiagnosticTimer;

export function beforeWebStart(options) {
  options.circuit ??= {};
  options.circuit.circuitHandlers ??= [];

  const hiddenDelay = options.circuit.autoPauseHiddenDelayMilliseconds ?? 120_000;
  console.log(
    `[auto-pause] initialized; document visibility is ${document.visibilityState}; ` +
    `hidden delay is ${hiddenDelay} ms.`);

  document.addEventListener('visibilitychange', () => {
    console.log(`[auto-pause] visibility changed to ${document.visibilityState}.`);

    clearTimeout(hiddenDiagnosticTimer);
    if (document.visibilityState === 'hidden') {
      console.log(`[auto-pause] hidden timer started; stay on another browser tab for at least ${hiddenDelay} ms.`);
      hiddenDiagnosticTimer = setTimeout(async () => {
        const active = document.activeElement;
        const dirtyFocusedInput =
          (active instanceof HTMLInputElement || active instanceof HTMLTextAreaElement) &&
          active.value !== active.defaultValue;
        const playingMedia = [...document.querySelectorAll('audio, video')]
          .some(element => !element.paused && !element.muted && element.volume > 0);
        const heldLocks = await navigator.locks?.query()
          .then(snapshot => snapshot.held?.map(item => item.name) ?? [])
          .catch(() => ['(query failed)']) ?? [];

        console.log('[auto-pause] hidden delay elapsed; blocker snapshot:', {
          visibilityState: document.visibilityState,
          circuitBusy,
          activeElement: active?.id || active?.tagName,
          dirtyFocusedInput,
          playingMedia,
          pictureInPicture: Boolean(document.pictureInPictureElement),
          heldLocks,
        });
      }, hiddenDelay);
    }
  });

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

export function afterWebStarted(blazor) {
  blazor.addEventListener?.('circuitactivitychanged', event => {
    circuitBusy = event.busy;
    console.log(`[auto-pause] circuit activity changed: busy=${circuitBusy}.`);
  });
}
