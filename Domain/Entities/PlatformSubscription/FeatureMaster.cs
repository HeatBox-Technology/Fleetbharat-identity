using System;
public class FeatureMaster
{
    public Guid FeatureId { get; set; } = Guid.NewGuid();
    public string FeatureCode { get; set; } = "";
    public string FeatureName { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
