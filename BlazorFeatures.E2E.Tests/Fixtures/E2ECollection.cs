using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Xunit;

namespace BlazorFeatures.E2E.Tests.Fixtures;

// xUnit v3 collection that shares a single ServerFixture (and therefore a
// single YARP proxy + app-process pool) across every test class that opts in
// via [Collection(nameof(E2ECollection))].
[CollectionDefinition(nameof(E2ECollection))]
public class E2ECollection : ICollectionFixture<ServerFixture<E2ETestAssembly>>;
