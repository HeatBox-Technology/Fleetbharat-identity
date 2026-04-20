using System;

public class ServiceVendorDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ServiceVendorSummaryDto
{
    public int TotalEntities { get; set; }
    public int Enabled { get; set; }
    public int Disabled { get; set; }
}

public class ServiceVendorListUiResponseDto
{
    public ServiceVendorSummaryDto Summary { get; set; } = new();
    public PagedResultDto<ServiceVendorDto> Vendors { get; set; } = new();
}
