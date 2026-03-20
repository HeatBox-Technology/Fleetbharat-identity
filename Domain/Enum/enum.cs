public enum PricingModelType
{
    Fixed = 1,
    LicenseBased = 2
}
public enum GeofenceGeometryType
{
    CIRCLE,
    POLYGON
}

public enum GeofenceStatus
{
    ENABLED,
    DISABLED
}
public static class SyncStatuses
{
    public const string Pending = "PENDING";
    public const string Synced = "SYNCED";
    public const string Failed = "FAILED";
}