# Suggested Release Notes Improvements for IComponentPropertyActivator

## Issue 1: Interface Signature is Incorrect

The release notes show:
```csharp
Action<IServiceProvider, object> GetActivator(...)
```

But the actual interface is:
```csharp
Action<IServiceProvider, IComponent> GetActivator(...)
```

The second parameter is `IComponent`, not `object`.

---

## Issue 2: Missing Practical Example

The current sample just shows empty placeholder comments. A real working example would be more helpful.

---

## Suggested Replacement Content

```markdown
## `IComponentPropertyActivator` for custom property injection

Blazor now provides `IComponentPropertyActivator` for customizing how `[Inject]` properties are populated on components. This enables advanced scenarios like:

- **Blazor Hybrid**: Custom context for property resolution in .NET MAUI
- **Custom DI containers**: Integration with Autofac, Simple Injector, etc.
- **Logging/diagnostics**: Log all service resolutions for debugging
- **Lazy injection**: Wrap services in lazy proxies
- **Feature flags**: Conditionally inject different implementations

```csharp
public interface IComponentPropertyActivator
{
    Action<IServiceProvider, IComponent> GetActivator(
        [DynamicallyAccessedMembers(Component)] Type componentType);
}
```

**Example: Logging property activator**

```csharp
public class LoggingPropertyActivator : IComponentPropertyActivator
{
    private readonly ILogger<LoggingPropertyActivator> _logger;
    private readonly ConcurrentDictionary<Type, Action<IServiceProvider, IComponent>> _cache = new();

    public LoggingPropertyActivator(ILogger<LoggingPropertyActivator> logger)
    {
        _logger = logger;
    }

    public Action<IServiceProvider, IComponent> GetActivator(Type componentType)
    {
        return _cache.GetOrAdd(componentType, type =>
        {
            var injectProperties = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.GetCustomAttribute<InjectAttribute>() != null)
                .ToList();

            return (serviceProvider, component) =>
            {
                foreach (var property in injectProperties)
                {
                    var injectAttr = property.GetCustomAttribute<InjectAttribute>();
                    object? service = injectAttr?.Key != null
                        ? serviceProvider.GetKeyedService(property.PropertyType, injectAttr.Key)
                        : serviceProvider.GetService(property.PropertyType);

                    if (service != null)
                    {
                        _logger.LogDebug("Injecting {Service} into {Component}.{Property}",
                            property.PropertyType.Name, type.Name, property.Name);
                        property.SetValue(component, service);
                    }
                }
            };
        });
    }
}

// Registration - replaces default property injection
builder.Services.AddSingleton<IComponentPropertyActivator, LoggingPropertyActivator>();
```

The default implementation caches activators per component type, supports keyed services via `[Inject(Key = "...")]`, integrates with Hot Reload for cache invalidation, and includes proper trimming annotations for AOT compatibility.
```

---

## Summary of Suggested Changes

1. **Fix the interface signature**: Change `object` to `IComponent`
2. **Add practical use cases**: List concrete scenarios where this is useful
3. **Provide a working example**: Show a logging activator that actually does something
4. **Show keyed service support**: Demonstrate `[Inject(Key = "...")]` handling
5. **Emphasize caching**: Note that implementations should cache the activator delegates

---

## Sample Code Location

A working sample is available at:
https://github.com/danroth27/AspNetCore11Samples/tree/main/BlazorFeatures/BlazorFeatures/Services

Files:
- `LoggingPropertyActivator.cs` - Full implementation with logging
- `SampleService.cs` - Sample services for testing
- `PropertyActivatorDemo.razor` - Demo component showing the feature
