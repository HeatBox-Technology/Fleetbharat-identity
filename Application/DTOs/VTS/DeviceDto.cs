using System;


/// <summary>
/// DTO for device details (IMEI, type, SIM, etc.).
/// Example: { "AccountId": 1, "DeviceImeiOrSerial": "123456789012345", "DeviceTypeId": 2, "DeviceStatus": "Active" }
/// </summary>
public class DeviceDto
{
    /// <summary>Unique device ID</summary>
    public int Id { get; set; }
    /// <summary>Account ID (FK to mst_account)</summary>
    public int AccountId { get; set; }
    /// <summary>Device IMEI or serial number (unique)</summary>
    public int ManufactureID { get; set; }

    /// <summary>Device type ID (FK to mst_device_type)</summary>
    public int DeviceTypeId { get; set; }
    public string DeviceNo { get; set; } = string.Empty;
    public string DeviceImeiOrSerial { get; set; } = string.Empty;
    public string DeviceStatus { get; set; } = "Active";
    public int createdBy { get; set; }
    public DateTime createdAt { get; set; }
    public int? updatedBy { get; set; }
    public DateTime? updatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class DeviceCardSummaryDto
{
    public int TotalDevices { get; set; }
    public int InService { get; set; }
    public int OutOfService { get; set; }
}
public class DeviceListUiResponseDto
{
    public DeviceCardSummaryDto Summary { get; set; } = new();

    public PagedResultDto<DeviceDto> Devices { get; set; } = new();
}

