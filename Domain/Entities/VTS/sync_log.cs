using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("map_vehicle_device_sync_log")]
public class map_vehicle_device_sync_log
{
    [Key]
    public int Id { get; set; }

    public int MappingId { get; set; }

    public string PayloadJson { get; set; } = "";

    public bool IsSynced { get; set; }

    public int RetryCount { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastTriedAt { get; set; }
}
[Table("map_geofence_sync_log")]
public class map_geofence_sync_log
{
    public int Id { get; set; }

    public int GeofenceId { get; set; }

    public string PayloadJson { get; set; } = string.Empty;

    public bool IsSynced { get; set; }

    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }

    public DateTime LastTriedAt { get; set; } = DateTime.UtcNow;
}
[Table("map_vehicle_geofence_sync_log")]
public class map_vehicle_geofence_sync_log
{
    public int Id { get; set; }
    public int MappingId { get; set; }
    public string PayloadJson { get; set; }
    public bool IsSynced { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime LastTriedAt { get; set; }
}