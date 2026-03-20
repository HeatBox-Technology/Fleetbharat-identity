using System;

/// <summary>
/// DTO for vehicle-device mapping.
/// </summary>
public class VehicleDeviceMapDto
{
    public int Id { get; set; }

    public int AccountId { get; set; }

    public int VehicleId { get; set; }

    public int DeviceId { get; set; }

    public int DeviceTypeId { get; set; }

    public int? SimId { get; set; }

    public string? SimNumber { get; set; }

    public string? Remarks { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime InstallationDate { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }
    public string? VehicleNo { get; set; }
    public string? DeviceNo { get; set; }
    public string? DeviceTypeName { get; set; }
}
public class CreateVehicleDeviceMapDto
{
    public int AccountId { get; set; }
    public int VehicleId { get; set; }
    public int DeviceId { get; set; }
    public int DeviceTypeId { get; set; }
    public int SimId { get; set; }
    public string? SimNumber { get; set; }
    public string? Remarks { get; set; }
    public int CreatedBy { get; set; }
}
public class UpdateVehicleDeviceMapDto
{
    public int VehicleId { get; set; }
    public int DeviceId { get; set; }
    public int DeviceTypeId { get; set; }
    public int SimId { get; set; }
    public string? SimNumber { get; set; }
    public string? Remarks { get; set; }
    public bool IsActive { get; set; }
    public int? UpdatedBy { get; set; }
}

public class VehicleDeviceAssignmentSummaryDto
{
    public int TotalAssignments { get; set; }

    public int Active { get; set; }

    public int WithIssues { get; set; }
}
public class VehicleDeviceAssignmentListUiResponseDto
{
    public VehicleDeviceAssignmentSummaryDto Summary { get; set; } = new();

    public PagedResultDto<VehicleDeviceMapDto> Assignments { get; set; } = new();
}
