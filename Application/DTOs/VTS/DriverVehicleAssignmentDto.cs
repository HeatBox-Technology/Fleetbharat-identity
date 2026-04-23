using System;

public class DriverVehicleAssignmentDto
{
    public int Id { get; set; }
    public int AccountContextId { get; set; }
    public int DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public int VehicleId { get; set; }
    public string VehicleNumber { get; set; } = string.Empty;
    public string AssignmentLogic { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? ExpectedEnd { get; set; }
    public string? DispatcherNotes { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? CreatedByUser { get; set; }
    public DateTime? CreatedAtUser { get; set; }
    public Guid? UpdatedByUser { get; set; }
    public DateTime? UpdatedAtUser { get; set; }
    public Guid? DeletedByUser { get; set; }
    public DateTime? DeletedAtUser { get; set; }
    public bool IsDeleted { get; set; }
}

public class CreateDriverVehicleAssignmentDto
{
    public int AccountContextId { get; set; }
    public int DriverId { get; set; }
    public int VehicleId { get; set; }
    public string AssignmentLogic { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? ExpectedEnd { get; set; }
    public string? DispatcherNotes { get; set; }
    public int CreatedBy { get; set; }
    public Guid? CreatedByUser { get; set; }
}

public class UpdateDriverVehicleAssignmentDto
{
    public int AccountContextId { get; set; }
    public int DriverId { get; set; }
    public int VehicleId { get; set; }
    public string AssignmentLogic { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? ExpectedEnd { get; set; }
    public string? DispatcherNotes { get; set; }
    public int? UpdatedBy { get; set; }
    public Guid? UpdatedByUser { get; set; }
}
