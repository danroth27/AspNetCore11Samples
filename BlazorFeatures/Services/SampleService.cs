// Sample services for demonstrating IComponentPropertyActivator

namespace BlazorFeatures.Services;

public interface ISampleService
{
    string GetMessage();
    string ServiceType { get; }
}

public class SampleService : ISampleService
{
    public string GetMessage() => $"Hello from SampleService at {DateTime.Now:HH:mm:ss}";
    public string ServiceType => "Standard";
}

public class PremiumSampleService : ISampleService
{
    public string GetMessage() => $"Hello from PREMIUM SampleService at {DateTime.Now:HH:mm:ss}";
    public string ServiceType => "Premium";
}
