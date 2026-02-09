// =============================================================================
// FEATURE: IComponentPropertyActivator for custom property injection
// This sample demonstrates a custom property activator that adds logging
// and supports an additional [InjectWithLogging] attribute.
// =============================================================================

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace BlazorFeatures.Services;

/// <summary>
/// Custom attribute that injects a service and logs the injection.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class InjectWithLoggingAttribute : Attribute
{
}

/// <summary>
/// A custom property activator that logs all property injections.
/// This demonstrates the IComponentPropertyActivator extensibility point.
/// </summary>
public class LoggingPropertyActivator : IComponentPropertyActivator
{
    private readonly ILogger<LoggingPropertyActivator> _logger;
    private readonly ConcurrentDictionary<Type, Action<IServiceProvider, IComponent>> _activatorCache = new();

    public LoggingPropertyActivator(ILogger<LoggingPropertyActivator> logger)
    {
        _logger = logger;
    }

    public Action<IServiceProvider, IComponent> GetActivator(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType)
    {
        return _activatorCache.GetOrAdd(componentType, CreateActivator);
    }

    private Action<IServiceProvider, IComponent> CreateActivator(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType)
    {
        // Find all properties with [Inject] or [InjectWithLogging] attributes
        var injectableProperties = componentType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(p => p.GetCustomAttribute<InjectAttribute>() != null ||
                        p.GetCustomAttribute<InjectWithLoggingAttribute>() != null)
            .ToList();

        if (injectableProperties.Count == 0)
        {
            return static (_, _) => { };
        }

        return (serviceProvider, component) =>
        {
            foreach (var property in injectableProperties)
            {
                var serviceType = property.PropertyType;
                var injectAttr = property.GetCustomAttribute<InjectAttribute>();
                
                // Check for keyed service injection
                object? service;
                if (injectAttr?.Key != null)
                {
                    service = serviceProvider.GetKeyedService(serviceType, injectAttr.Key);
                    _logger.LogDebug(
                        "Injecting keyed service {ServiceType} with key '{Key}' into {ComponentType}.{PropertyName}",
                        serviceType.Name, injectAttr.Key, componentType.Name, property.Name);
                }
                else
                {
                    service = serviceProvider.GetService(serviceType);
                    _logger.LogDebug(
                        "Injecting {ServiceType} into {ComponentType}.{PropertyName}",
                        serviceType.Name, componentType.Name, property.Name);
                }

                if (service == null)
                {
                    // Check if it's required (no default value possible for reference types)
                    var isRequired = !property.PropertyType.IsValueType && 
                                     property.GetCustomAttribute<InjectAttribute>() != null;
                    
                    if (isRequired)
                    {
                        throw new InvalidOperationException(
                            $"Cannot provide a value for property '{property.Name}' on type '{componentType.FullName}'. " +
                            $"There is no registered service of type '{serviceType}'.");
                    }
                }
                else
                {
                    property.SetValue(component, service);
                }
            }
        };
    }
}
