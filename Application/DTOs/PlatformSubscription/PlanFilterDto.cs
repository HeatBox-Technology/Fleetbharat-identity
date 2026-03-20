
public class PlanFilterDto
{
    public string? Search { get; set; }
    public string? TenantCategory { get; set; }

    public PricingModelType? PricingModel { get; set; }

    public bool? IsActive { get; set; }
    public int? FormModuleId { get; set; }
}
