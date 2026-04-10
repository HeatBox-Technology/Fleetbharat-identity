using System;
using System.Collections.Generic;

public class VehicleGeofenceMapDto
{
    public int Id { get; set; }

    public int AccountId { get; set; }

    public int VehicleId { get; set; }

    public int GeofenceId { get; set; }

    public string? VehicleNo { get; set; }

    public string? GeofenceName { get; set; }

    public string? GeometryType { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public string? Remarks { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
public class CreateVehicleGeofenceMapDto
{
    public int AccountId { get; set; }

    public int VehicleId { get; set; }

    // public int GeofenceId { get; set; }
    public List<int> GeofenceIds { get; set; } = new();

    public string? Remarks { get; set; }

    public int CreatedBy { get; set; }
}
public class UpdateVehicleGeofenceMapDto
{
    public int VehicleId { get; set; }

    public int GeofenceId { get; set; }

    public string? Remarks { get; set; }

    public bool IsActive { get; set; }

    public int? UpdatedBy { get; set; }
}
public class VehicleGeofenceAssignmentSummaryDto
{
    public int TotalAssignments { get; set; }

    public int Active { get; set; }

    public int Inactive { get; set; }
}
public class VehicleGeofenceAssignmentListUiResponseDto
{
    public VehicleGeofenceAssignmentSummaryDto Summary { get; set; } = new();

    public PagedResultDto<VehicleGeofenceMapDto> Assignments { get; set; } = new();
}