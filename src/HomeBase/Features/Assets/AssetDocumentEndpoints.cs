namespace HomeBase.Features.Assets;

public static class AssetDocumentEndpoints
{
    public static IEndpointRouteBuilder MapAssetDocumentEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        endpoints.MapGet(
            "/documents/{id:int}",
            async (int id, IAssetService assets, IAssetDocumentStore store, CancellationToken ct) =>
            {
                var document = await assets.FindDocumentAsync(id, ct);

                if (document is null || store.Resolve(document.FilePath) is not { } path)
                {
                    return Results.NotFound();
                }

                return Results.File(
                    path,
                    AssetDocumentStore.ContentTypeOf(document.FileName),
                    enableRangeProcessing: true
                );
            }
        );

        return endpoints;
    }
}
