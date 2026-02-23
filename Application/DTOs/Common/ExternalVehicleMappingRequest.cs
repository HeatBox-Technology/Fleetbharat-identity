public class ExternalVehicleMappingRequest
{
    public int vehicleid { get; set; }
    public string VehicleNo { get; set; } = "";
    public string DeviceNo { get; set; } = "";
    public string Imei { get; set; } = "";
    public string DeviceType { get; set; } = "";
    public string OrgName { get; set; } = "";
    public int OrgId { get; set; }

    public int SpeedLimit { get; set; } = 0;

    public bool Overspeed { get; set; } = true;
    public bool PowerCut { get; set; } = true;
    public bool LowPower { get; set; } = true;
    public bool DoorClose { get; set; } = true;
    public bool DoorLock { get; set; } = true;
    public bool Collision { get; set; } = true;
    public bool Geofence { get; set; } = true;
    public bool Ac { get; set; } = true;
    public bool Ignition { get; set; } = true;
    public bool Sos { get; set; } = true;
    public bool Fatigue { get; set; } = true;
    public bool GnssFault { get; set; } = true;
    public bool GnssAntennaDisconnect { get; set; } = true;
    public bool GnssAntennaShort { get; set; } = true;
    public bool Rollover { get; set; } = true;
    public bool IdleStart { get; set; } = true;
    public int IdleStartDurationMin { get; set; } = 0;
    public bool IdleAc { get; set; } = true;
    public int IdleACDurationMin { get; set; } = 0;
    public bool Towing { get; set; } = true;
}