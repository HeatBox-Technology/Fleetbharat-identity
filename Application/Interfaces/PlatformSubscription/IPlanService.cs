
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IPlanService

{
    Task<Guid> CreateAsync(CreateMarketPlanDto dto);

    Task<PlanDetailResponseDto?> GetByIdAsync(Guid planId);

    Task<PagedResultDto<PlanListItemResponseDto>> GetPagedAsync(
        PagedRequestDto page,
        PlanFilterDto filter);

    Task<bool> UpdateAsync(Guid planId, CreateMarketPlanDto dto);

    Task<bool> DeleteAsync(Guid planId);

    Task<bool> UpdateStatusAsync(Guid planId, bool isActive);
}
