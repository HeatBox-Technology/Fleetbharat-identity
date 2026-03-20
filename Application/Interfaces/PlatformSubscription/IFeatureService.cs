using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IFeatureService
{
    Task<Guid> CreateAsync(FeatureMaster feature);
    Task<List<FeatureMaster>> GetAllAsync();
}
