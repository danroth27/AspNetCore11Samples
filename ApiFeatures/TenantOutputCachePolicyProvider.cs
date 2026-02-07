// =============================================================================
// Test: TenantOutputCachePolicyProvider from PR #10237 (latest version)
// This file tests whether the updated sample code from the release notes compiles
// =============================================================================

using Microsoft.AspNetCore.OutputCaching;

namespace ApiFeatures;

// Mock interface for the sample
public interface ITenantService
{
    ValueTask<TenantCacheSettings?> GetCacheSettingsAsync(string policyName);
}

// Mock settings class
public class TenantCacheSettings
{
    public TimeSpan Duration { get; set; }
    public bool VaryByQuery { get; set; }
}

// Mock custom policy implementation
public class CustomOutputCachePolicy : IOutputCachePolicy
{
    private readonly TenantCacheSettings _settings;
    
    public CustomOutputCachePolicy(TenantCacheSettings settings)
    {
        _settings = settings;
    }
    
    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        context.EnableOutputCaching = true;
        context.ResponseExpirationTimeSpan = _settings.Duration;
        return ValueTask.CompletedTask;
    }
    
    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
    
    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

// Sample from PR #10237 release notes (UPDATED VERSION)
public class TenantOutputCachePolicyProvider : IOutputCachePolicyProvider
{
    private readonly ITenantService _tenantService;
    private readonly Dictionary<string, IOutputCachePolicy> _cachedPolicies = new();
    
    public TenantOutputCachePolicyProvider(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }
    
    public IReadOnlyList<IOutputCachePolicy> GetBasePolicies()
    {
        // Return empty - no base policies in this custom implementation
        return Array.Empty<IOutputCachePolicy>();
    }
    
    public async ValueTask<IOutputCachePolicy?> GetPolicyAsync(string policyName)
    {
        // Check if we've already built this policy
        if (_cachedPolicies.TryGetValue(policyName, out var cachedPolicy))
        {
            return cachedPolicy;
        }
        
        // Load policy settings from tenant configuration
        var tenantSettings = await _tenantService.GetCacheSettingsAsync(policyName);
        
        if (tenantSettings == null)
        {
            return null;
        }
        
        // Build a custom policy implementation
        var policy = new CustomOutputCachePolicy(tenantSettings);
        _cachedPolicies[policyName] = policy;
        
        return policy;
    }
}
