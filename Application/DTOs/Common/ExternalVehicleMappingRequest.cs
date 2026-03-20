using System.Collections.Generic;
using System.Text.Json.Serialization;

public class ExternalVehicleMappingRequest
{
    [JsonPropertyName("vehicleId")]
    public string VehicleId { get; set; } = string.Empty;

    [JsonPropertyName("vehicleNo")]
    public string VehicleNo { get; set; } = "";

    [JsonPropertyName("deviceNo")]
    public string DeviceNo { get; set; } = "";

    [JsonPropertyName("imei")]
    public string Imei { get; set; } = "";

    [JsonPropertyName("deviceType")]
    public string DeviceType { get; set; } = "";

    [JsonPropertyName("orgName")]
    public string OrgName { get; set; } = "";

    [JsonPropertyName("orgId")]
    public int OrgId { get; set; }

    [JsonPropertyName("speedLimit")]
    public int SpeedLimit { get; set; } = 0;

    [JsonPropertyName("overspeed")]
    public bool Overspeed { get; set; } = true;

    [JsonPropertyName("powerCut")]
    public bool PowerCut { get; set; } = true;

    [JsonPropertyName("lowPower")]
    public bool LowPower { get; set; } = true;

    [JsonPropertyName("doorClose")]
    public bool DoorClose { get; set; } = true;

    [JsonPropertyName("doorLock")]
    public bool DoorLock { get; set; } = true;

    [JsonPropertyName("collision")]
    public bool Collision { get; set; } = true;

    [JsonPropertyName("geofence")]
    public bool Geofence { get; set; } = true;

    [JsonPropertyName("ac")]
    public bool Ac { get; set; } = true;

    [JsonPropertyName("ignition")]
    public bool Ignition { get; set; } = true;

    [JsonPropertyName("sos")]
    public bool Sos { get; set; } = true;

    [JsonPropertyName("fatigue")]
    public bool Fatigue { get; set; } = true;

    [JsonPropertyName("gnssFault")]
    public bool GnssFault { get; set; } = true;

    [JsonPropertyName("gnssAntennaDisconnect")]
    public bool GnssAntennaDisconnect { get; set; } = true;

    [JsonPropertyName("gnssAntennaShort")]
    public bool GnssAntennaShort { get; set; } = true;

    [JsonPropertyName("rollover")]
    public bool Rollover { get; set; } = true;

    [JsonPropertyName("idleStart")]
    public bool IdleStart { get; set; } = true;

    [JsonPropertyName("idleStartDurationMin")]
    public int IdleStartDurationMin { get; set; } = 0;

    [JsonPropertyName("idleAc")]
    public bool IdleAc { get; set; } = true;

    [JsonPropertyName("idleACDurationMin")]
    public int IdleACDurationMin { get; set; } = 0;

    [JsonPropertyName("towing")]
    public bool Towing { get; set; } = true;
}
public class ExternalGeofenceRequest
{
    public string GeoId { get; set; } = string.Empty;
    public string GeoName { get; set; } = string.Empty;
    public int OrgId { get; set; }
    public string OrgName { get; set; } = string.Empty;

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Radius { get; set; }

    public string GeoType { get; set; } = "CIRCLE";

    public List<ExternalGeoPoint> GeoPoints { get; set; } = new();
}

public class ExternalGeoPoint
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
public class ExternalGeofenceMappingRequest
{
    public string vehicleId { get; set; }
    public string vehicleNo { get; set; }
    public string deviceNo { get; set; }

    public List<ExternalGeofenceItem> geofence { get; set; } = new();
}

public class ExternalGeofenceItem
{
    public int geoId { get; set; }
    public string tripNo { get; set; } = "0";
    public string geoPoint { get; set; } = "START";
}
