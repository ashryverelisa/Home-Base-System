namespace HomeBase.Features.Ingest;

public interface IRecipeIngestService
{
    Task<RecipeIngestResult?> IngestAsync(RecipeRequest request, CancellationToken ct = default);
}
