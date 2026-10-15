using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: DoNotParallelize]

namespace BlazorFeatures.E2E.Tests.Fixtures;

[TestClass]
public class E2ETestAssembly
{
    public static ServerFactory<E2ETestAssembly> Servers { get; private set; } = null!;

    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        Servers = new ServerFactory<E2ETestAssembly>();
        await Servers.InitializeAsync();
    }

    [AssemblyCleanup]
    public static Task CleanupAsync() => Servers.DisposeAsync().AsTask();
}
