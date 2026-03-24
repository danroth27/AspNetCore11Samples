using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;

namespace BlazorWasmFeatures;

/// <summary>
/// Worker methods that run on a background Web Worker thread.
/// Methods marked with [JSExport] are callable from the worker's JavaScript context.
/// </summary>
[SupportedOSPlatform("browser")]
public static partial class DataWorker
{
    private static readonly string[] Regions = ["North America", "Europe", "Asia Pacific", "Latin America", "Middle East"];
    private static readonly string[] Products = ["Widget Pro", "Gadget X", "Service Plus", "Enterprise Suite", "Starter Pack",
                                                   "Analytics Tool", "Cloud Storage", "API Gateway", "Dev Platform", "Support Plan"];

    /// <summary>
    /// Generates a large dataset of sales records and computes summary statistics.
    /// This runs entirely on the worker thread, keeping the UI thread free.
    /// </summary>
    [JSExport]
    public static string AnalyzeSalesData(int recordCount)
    {
        // Generate the dataset
        var random = new Random(42);
        var records = new SalesRecord[recordCount];

        for (int i = 0; i < recordCount; i++)
        {
            records[i] = new SalesRecord(
                Id: $"ORD-{i:D7}",
                Product: Products[random.Next(Products.Length)],
                Region: Regions[random.Next(Regions.Length)],
                Amount: Math.Round(random.NextDouble() * 500 + 10, 2)
            );
        }

        // Analyze it
        var byRegion = records
            .GroupBy(r => r.Region)
            .Select(g => new RegionSummary(g.Key, g.Count(), Math.Round(g.Sum(r => r.Amount), 2)))
            .OrderByDescending(r => r.Revenue)
            .ToArray();

        var topProducts = records
            .GroupBy(r => r.Product)
            .Select(g => new { Product = g.Key, Revenue = g.Sum(r => r.Amount) })
            .OrderByDescending(p => p.Revenue)
            .Take(5)
            .Select(p => new ProductSummary(p.Product, Math.Round(p.Revenue, 2)))
            .ToArray();

        var result = new SalesAnalysis(
            TotalRecords: records.Length,
            TotalRevenue: Math.Round(records.Sum(r => r.Amount), 2),
            AverageOrderValue: Math.Round(records.Average(r => r.Amount), 2),
            RegionBreakdown: byRegion,
            TopProducts: topProducts
        );

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Same analysis logic for main-thread comparison.
    /// </summary>
    public static SalesAnalysis AnalyzeSalesDataLocal(int recordCount)
    {
        var resultJson = AnalyzeSalesData(recordCount);
        return JsonSerializer.Deserialize<SalesAnalysis>(resultJson)!;
    }
}

public record SalesRecord(string Id, string Product, string Region, double Amount);
public record RegionSummary(string Region, int Orders, double Revenue);
public record ProductSummary(string Product, double Revenue);
public record SalesAnalysis(
    int TotalRecords,
    double TotalRevenue,
    double AverageOrderValue,
    RegionSummary[] RegionBreakdown,
    ProductSummary[] TopProducts);
