using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using NetTopologySuite.Geometries;
using StackExchange.Redis;

public class mst_Geofence : IAccountEntity
{
    public int Id { get; set; }

    // Identity & Registry
    public int AccountId { get; set; }
    public string UniqueCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Classification
    public string ClassificationCode { get; set; } = string.Empty;
    public string? ClassificationLabel { get; set; }

    // Visualization
    public string? ColorTheme { get; set; }
    public decimal? Opacity { get; set; }

    // Geometry
    public string GeometryType { get; set; } = string.Empty;   // CIRCLE / POLYGON
    public int? RadiusM { get; set; }
    public Geometry Geom { get; set; } = default!;

    public JsonDocument? CoordinatesJson { get; set; }
    // Operational
    public string Status { get; set; } = "ENABLED";
    public bool IsDeleted { get; set; } = false;

    // Mongo Sync
    public string? MongoId { get; set; }
    public string SyncStatus { get; set; } = "PENDING";
    public DateTime? LastSyncedAt { get; set; }
    public string? SyncError { get; set; }

    // Concurrency
    public int Version { get; set; } = 1;

    // Audit
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
}
