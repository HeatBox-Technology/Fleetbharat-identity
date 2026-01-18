using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IPlanEntitlementService
{
    Task<bool> AssignAsync(Guid planId, List<Guid> featureIds);
    Task<List<Guid>> GetFeatureIdsAsync(Guid planId);
}