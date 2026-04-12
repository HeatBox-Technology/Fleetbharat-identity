using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IBillingInvoiceService
{
    Task<List<InvoiceResponseDto>> GetInvoicesAsync(int skip, int take, CancellationToken ct = default);
    Task<List<InvoiceResponseDto>> GetInvoicesByAccountAsync(int accountId, int skip, int take, CancellationToken ct = default);
    Task<InvoiceResponseDto> CreateManualInvoiceAsync(InvoiceManualCreateDto dto, CancellationToken ct = default);
    Task<bool> DeleteInvoiceAsync(int id, CancellationToken ct = default);
    Task<string> ExportInvoicesCsvAsync(int skip, int take, CancellationToken ct = default);
    Task<byte[]> ExportInvoicesXlsxAsync(int skip, int take, CancellationToken ct = default);
    Task<int> GenerateDueInvoicesBatchAsync(int take, CancellationToken ct = default);
}
