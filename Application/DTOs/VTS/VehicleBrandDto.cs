using System;

public class VehicleBrandDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class VehicleBrandSummaryDto
{
    public int TotalEntities { get; set; }
    public int Enabled { get; set; }
    public int Disabled { get; set; }
}

public class VehicleBrandListUiResponseDto
{
    public VehicleBrandSummaryDto Summary { get; set; } = new();
    public PagedResultDto<VehicleBrandDto> Brands { get; set; } = new();
}
