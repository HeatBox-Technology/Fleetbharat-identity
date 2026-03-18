public static class VtsExternalSyncModules
{
    public const string Geofence = "vts.geofence";
    public const string VehicleDeviceMap = "vts.vehicle-device-map";
    public const string VehicleGeofenceMap = "vts.vehicle-geofence-map";
}

public static class ExternalApiLogStatus
{
    public const string Pending = "Pending";
    public const string Success = "Success";
    public const string Failed = "Failed";
}

public class VtsExternalSyncEnvelope
{
    public long ExternalApiLogId { get; set; }
    public string Operation { get; set; } = "POST";
    public string PayloadJson { get; set; } = "";
}
