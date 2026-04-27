using System;

public class VehicleAccessForm
{
    public long Id { get; set; }
    public long VehicleAccessId { get; set; }
    public int FormId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public VehicleAccess? VehicleAccess { get; set; }
}
