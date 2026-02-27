using System;
using Domain.Entities;

public class map_vehicle_geofence
{
    public int Id { get; set; }

    public int AccountId { get; set; }

    public int VehicleId { get; set; }
    public int GeofenceId { get; set; }

    public string? Remarks { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public mst_vehicle? Vehicle { get; set; }
    public mst_Geofence? Geofence { get; set; }
    public string? SyncStatus { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public string? SyncError { get; set; }
}