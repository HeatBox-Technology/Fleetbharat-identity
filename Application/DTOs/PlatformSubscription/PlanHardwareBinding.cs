using System;
using System.Collections.Generic;

public class PlanHardwareBindingDto
{
    public bool IsHardwareLocked { get; set; }
    public List<Guid>? AllowedDeviceFamilyIds { get; set; }
}