using System;
using System.Collections.Generic;
using System.Threading.Tasks;
public interface IAddonService
{
    Task<Guid> CreateAsync(AddonMaster addon);
    Task<List<AddonMaster>> GetAllAsync();
}
