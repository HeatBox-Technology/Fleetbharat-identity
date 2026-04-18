using System;
using System.Collections.Generic;

public static class DeviceTransferStatuses
{
    public const string Pending = "Pending";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
}

public class CreateDeviceTransferRequest
{
    public int FromAccountId { get; set; }
    public int ToAccountId { get; set; }
    public List<int> DeviceIds { get; set; } = new();
    public string? Remarks { get; set; }
    public int CreatedBy { get; set; }
}

public class UpdateDeviceTransferStatusRequest
{
    public int? UpdatedBy { get; set; }
    public string? Remarks { get; set; }
}

public class DeviceTransferCreateResultDto
{
    public int TransferId { get; set; }
    public string TransferCode { get; set; } = string.Empty;
    public string Status { get; set; } = DeviceTransferStatuses.Pending;
}

public class DeviceTransferActionResultDto
{
    public int TransferId { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class DeviceTransferItemDto
{
    public int DeviceId { get; set; }
    public string DeviceNo { get; set; } = string.Empty;
    public string DeviceImeiOrSerial { get; set; } = string.Empty;
}

public class DeviceTransferDto
{
    public int Id { get; set; }
    public string TransferCode { get; set; } = string.Empty;
    public int FromAccountId { get; set; }
    public string FromAccountName { get; set; } = string.Empty;
    public int ToAccountId { get; set; }
    public string ToAccountName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int DeviceCount { get; set; }
    public List<DeviceTransferItemDto> Items { get; set; } = new();
}

public class DeviceTransferListItemDto
{
    public int Id { get; set; }
    public string TransferCode { get; set; } = string.Empty;
    public int FromAccountId { get; set; }
    public string FromAccountName { get; set; } = string.Empty;
    public int ToAccountId { get; set; }
    public string ToAccountName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public int DeviceCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class DeviceTransferSummaryDto
{
    public int TotalTransfers { get; set; }
    public int Pending { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
}

public class DeviceTransferListUiResponseDto
{
    public DeviceTransferSummaryDto Summary { get; set; } = new();
    public PagedResultDto<DeviceTransferListItemDto> Transfers { get; set; } = new();
}
