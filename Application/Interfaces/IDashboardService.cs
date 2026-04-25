using System.Threading;
using System.Threading.Tasks;

public interface IDashboardService
{
    Task<DashboardSummaryResponseDto> GetSummaryAsync(DashboardSummaryRequestDto request, CancellationToken ct = default);
}
