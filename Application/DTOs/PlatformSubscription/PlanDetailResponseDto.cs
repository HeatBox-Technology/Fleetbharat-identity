using System;
using System.Collections.Generic;

public class PlanDetailResponseDto
{
    public Guid PlanId { get; set; }

    public PlanStructuralDefinitionDto Structure { get; set; } = new();
    public PlanSetupFeeDto SetupFee { get; set; } = new();
    public PlanRecurringFeeDto RecurringFee { get; set; } = new();
    public PlanHardwareBindingDto HardwareBinding { get; set; } = new();
    public PlanUserLimitDto UserLimits { get; set; } = new();
    public PlanSupportLineDto Support { get; set; } = new();
    //public PlanPricingDto Pricing { get; set; } = new();
    public PlanAdminGuardDto AdminGuard { get; set; } = new();
    public List<PlanUnitLicenseDto>? UnitLicenses { get; set; }

    public List<Guid> FeatureIds { get; set; } = new();
    public List<Guid> AddonIds { get; set; } = new();

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}