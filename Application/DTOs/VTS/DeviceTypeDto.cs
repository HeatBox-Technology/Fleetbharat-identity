using System;

public class DeviceTypeDto
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsEnabled { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class DeviceTypeSummaryDto
{
    public int TotalEntities { get; set; }
    public int Enabled { get; set; }
    public int Disabled { get; set; }
}

public class DeviceTypeListUiResponseDto
{
    public DeviceTypeSummaryDto Summary { get; set; } = new();

    public PagedResultDto<DeviceTypeDto> DeviceTypes { get; set; } = new();
}