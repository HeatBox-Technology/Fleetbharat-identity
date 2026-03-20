using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IPlanAddonService
{
    Task<bool> AssignAsync(Guid planId, List<Guid> addonIds);
    Task<List<Guid>> GetAddonIdsAsync(Guid planId);
}