using System;


/// <summary>
/// DTO for vehicle-device mapping.
/// </summary>
public class VehicleDeviceMapDto
{
    public int Id { get; set; }

    public int AccountId { get; set; }

    public int Fk_VehicleId { get; set; }


    public int fk_devicetypeid { get; set; }

    public int Fk_DeviceId { get; set; }

    public int fk_simid { get; set; }

    public string simnno { get; set; } = string.Empty;

    public string? Remarks { get; set; }

    public int IsActive { get; set; } = 1;

    public int IsDeleted { get; set; } = 0;

    public DateTime InstallationDate { get; set; }

    public int createdBy { get; set; }

    public DateTime createdAt { get; set; }

    public int? updatedBy { get; set; }

    public DateTime? updatedAt { get; set; }
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
