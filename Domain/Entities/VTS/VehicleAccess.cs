using System;
using System.Collections.Generic;

public class VehicleAccess : IAccountEntity
{
    public long Id { get; set; }
    public int AccountId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int VehicleId { get; set; }
    public DateTime AccessStartDate { get; set; }
    public DateTime? AccessEndDate { get; set; }
    public bool CanViewTracking { get; set; }
    public bool CanViewReports { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public ICollection<VehicleAccessForm> Forms { get; set; } = new List<VehicleAccessForm>();
}
