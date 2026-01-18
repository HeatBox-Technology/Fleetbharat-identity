using System;
using System.Collections.Generic;

public class PlanAddonAssignmentDto
{
    public Guid PlanId { get; set; }
    public List<Guid> AddonIds { get; set; }
}