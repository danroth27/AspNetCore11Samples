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
let hiddenDelay = 120_000;
let circuitHandlerRegistered = false;

export function beforeWebStart(options) {
  options.circuit ??= {};

  // Preview 7 workaround: WithBrowserOptions serializes these extension values in the
  // Blazor-Configuration marker, but they aren't copied into the circuit options passed
  // to JS initializers. The AutoPause package initializer runs after this app initializer
  // and reads them here, so provide the same values only when they're absent. Remove this
  // fallback once the framework flows extensions into the initializer options.
  options.circuit.autoPauseEnabled ??= true;
  options.circuit.autoPauseHiddenDelayMilliseconds ??= 10_000;

  hiddenDelay = options.circuit.autoPauseHiddenDelayMilliseconds ?? 120_000;
  console.log(
    `[auto-pause] initializer loaded; document visibility is ${document.visibilityState}; ` +
    `hidden delay is ${hiddenDelay} ms.`);

  options.circuit.circuitHandlers ??= [];
  if (!circuitHandlerRegistered) {
    circuitHandlerRegistered = true;
    options.circuit.circuitHandlers.push({
      onCircuitPausing: handleCircuitPausing,
      onCircuitOpened: () => console.log('[circuit] opened'),
      onCircuitClosed: () => console.log('[circuit] closed'),
    });
  }

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

}

export function afterWebStarted(blazor) {
  blazor.addEventListener?.('circuitactivitychanged', event => {
    circuitBusy = event.busy;
  });
}

async function handleCircuitPausing(signal) {
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
}
