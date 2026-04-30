using System;

public class DeviceModelResponseDto
{
    public int Id { get; set; }
    public int ManufacturerId { get; set; }
    public string ManufacturerName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DeviceCategoryId { get; set; }
    public string DeviceCategoryName { get; set; } = string.Empty;
    public bool UseIMEIAsPrimaryId { get; set; }
    public string? DeviceNo { get; set; }
    public string? IMEISerialNumber { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateDeviceModelRequestDto
{
    public int ManufacturerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DeviceCategoryId { get; set; }
    public bool UseIMEIAsPrimaryId { get; set; }
    public string? DeviceNo { get; set; }
    public string? IMEISerialNumber { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class UpdateDeviceModelRequestDto : CreateDeviceModelRequestDto
{
    public int Id { get; set; }
}

public class DeviceModelGetAllRequestDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public int? ManufacturerId { get; set; }
    public int? DeviceCategoryId { get; set; }
    public bool? IsEnabled { get; set; }
}
