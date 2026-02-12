using System;

namespace Application.DTOs
{
    /// <summary>
    /// DTO for device live tracking and current location data from Redis.
    /// </summary>
    public class DeviceLiveTrackingDto
    {
        public string DeviceNo { get; set; } = string.Empty;
        public string Imei { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Speed { get; set; }
        public double Altitude { get; set; }
        public int Direction { get; set; }
        public int? Rpm { get; set; }
        public string? NorthSouthLatitude { get; set; }
        public string? EastWestLongitude { get; set; }
        public bool Ignition { get; set; }
        public bool Ac { get; set; }
        public bool PowerCut { get; set; }
        public bool LowVoltage { get; set; }
        public bool DoorLock { get; set; }
        public bool DoorOpen { get; set; }
        public bool DeviceLock { get; set; }
        public bool FuelCut { get; set; }
        public bool GpsFixed { get; set; }
        public bool Collision { get; set; }
        public DateTime GpsDate { get; set; }
        public bool Sos { get; set; }
        public bool OverSpeed { get; set; }
        public bool Fatigue { get; set; }
        public bool Danger { get; set; }
        public bool GnssFault { get; set; }
        public bool GnssAntennaDisconnect { get; set; }
        public bool GnssAntennaShort { get; set; }
        public bool PowerUnderVoltage { get; set; }
        public bool PowerDown { get; set; }
        public bool PowerDisplayFault { get; set; }
        public bool TtsFault { get; set; }
        public bool Rollover { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public string Id { get; set; } = string.Empty;
        public string VehicleNo { get; set; } = string.Empty;
    }
}
