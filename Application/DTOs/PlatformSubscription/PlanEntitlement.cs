using System;
using System.Collections.Generic;

public class PlanEntitlementDto
{
    public Guid PlanId { get; set; }
    public List<Guid> FeatureIds { get; set; }
}