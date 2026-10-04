namespace HomeBase.Features.Ingest;

public interface IReceiptIngestService
{
    Task<ReceiptIngestResult?> IngestAsync(ReceiptRequest request, CancellationToken ct = default);
}
