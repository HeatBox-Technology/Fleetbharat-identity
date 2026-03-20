using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IBillingPlanService
{
    Task<List<PlanResponseDto>> GetPlansAsync(int skip, int take, CancellationToken ct = default);
    Task<PlanResponseDto?> GetPlanByIdAsync(int id, CancellationToken ct = default);
    Task<int> CreatePlanAsync(CreatePlanDto dto, CancellationToken ct = default);
    Task<bool> UpdatePlanAsync(int id, UpdatePlanDto dto, CancellationToken ct = default);
    Task<bool> DeletePlanAsync(int id, CancellationToken ct = default);
    Task<bool> UpsertPlanFeaturesAsync(int planId, PlanFeatureUpsertDto dto, CancellationToken ct = default);
}
