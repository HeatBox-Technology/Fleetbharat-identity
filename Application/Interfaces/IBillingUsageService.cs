using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IBillingUsageService
{
    Task<int> RecordUsageAsync(UsageRecordCreateDto dto, CancellationToken ct = default);
    Task<List<UsageRecordResponseDto>> GetUsageByAccountAsync(int accountId, int skip, int take, CancellationToken ct = default);
}
