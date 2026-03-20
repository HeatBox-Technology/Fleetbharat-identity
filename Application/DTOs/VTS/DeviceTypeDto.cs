using System;

public class DeviceTypeDto
{
    public int Id { get; set; }

    public int oemmanufacturerId { get; set; }
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Device type name (Display name for UI)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsEnabled { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
public class CreateDeviceTypeDto
{
    public string Code { get; set; } = string.Empty;
    public int oemmanufacturerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int CreatedBy { get; set; }
}
public class UpdateDeviceTypeDto
{
    public int Id { get; set; }
    public int oemmanufacturerId { get; set; }
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsEnabled { get; set; }

    public bool IsActive { get; set; }

    public int? UpdatedBy { get; set; }
}
public class DeviceTypeSummaryDto
{
    public int TotalDeviceTypes { get; set; }

    public int Enabled { get; set; }

    public int Disabled { get; set; }
}
public class DeviceTypeListUiResponseDto
{
    public DeviceTypeSummaryDto Summary { get; set; } = new();

    public PagedResultDto<DeviceTypeDto> DeviceTypes { get; set; } = new();
}