// =============================================================================
// FEATURE: IHostedService support in Blazor WebAssembly
// Blazor WebAssembly now supports IHostedService for running background services
// =============================================================================

using Microsoft.Extensions.Hosting;

namespace BlazorWasmFeatures.Services;

/// <summary>
/// Background service that runs in the browser using IHostedService.
/// This demonstrates the new .NET 11 Preview 1 feature for WebAssembly.
/// </summary>
public class DataRefreshService : IHostedService, IDisposable
{
    private Timer? _timer;
    private int _refreshCount = 0;

    public int RefreshCount => _refreshCount;

    public event Action? OnRefresh;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("[DataRefreshService] Starting background service...");
        
        // Refresh every 10 seconds for demo purposes
        _timer = new Timer(RefreshData, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        
        return Task.CompletedTask;
    }

    private void RefreshData(object? state)
    {
        _refreshCount++;
        Console.WriteLine($"[DataRefreshService] Data refresh #{_refreshCount} at {DateTime.Now:HH:mm:ss}");
        OnRefresh?.Invoke();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("[DataRefreshService] Stopping background service...");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
