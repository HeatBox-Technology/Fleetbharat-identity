using System;

public class DeviceModelDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ManufacturerId { get; set; }
    public string ManufacturerName { get; set; } = string.Empty;
    public int DeviceCategoryId { get; set; }
    public string DeviceCategoryName { get; set; } = string.Empty;
    public string ProtocolType { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateDeviceModelDto
{
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ManufacturerId { get; set; }
    public int DeviceCategoryId { get; set; }
    public string ProtocolType { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

public class UpdateDeviceModelDto : CreateDeviceModelDto
{
}

public class DeviceModelSummaryDto
{
    public int TotalEntities { get; set; }
    public int Enabled { get; set; }
    public int Disabled { get; set; }
}

public class DeviceModelListUiResponseDto
{
    public DeviceModelSummaryDto Summary { get; set; } = new();
    public PagedResultDto<DeviceModelDto> Models { get; set; } = new();
}
