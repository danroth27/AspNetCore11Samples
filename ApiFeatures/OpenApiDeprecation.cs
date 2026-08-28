namespace ApiFeatures;

public static class OpenApiDeprecation
{
    public static void MapOpenApiDeprecation(this WebApplication app)
    {
        app.MapGet("/catalog/{id}", GetCatalogItem)
            .WithName("GetCatalogItem");

#pragma warning disable CS0618 // The obsolete API is intentional demonstration code.
        app.MapGet("/catalog/legacy/{id}", GetLegacyCatalogItem)
            .WithName("GetLegacyCatalogItem");
#pragma warning restore CS0618
    }

    private static CatalogItem GetCatalogItem(int id) =>
        new(id, $"Product {id}", $"SKU-{id:D4}");

    [Obsolete("Use /catalog/{id}.")]
    private static LegacyCatalogItem GetLegacyCatalogItem(int id) =>
        new(id, $"Product {id}", $"SKU-{id:D4}");
}

public sealed record CatalogItem(int Id, string Name, string StockKeepingUnit);

[Obsolete("Use CatalogItem.")]
public sealed record LegacyCatalogItem(
    int Id,
    string Name,
    [property: Obsolete("Use StockKeepingUnit.")] string Sku);
