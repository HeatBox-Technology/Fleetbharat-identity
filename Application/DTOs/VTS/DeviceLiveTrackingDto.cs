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

    // 👇 Raw array mapping
    [JsonProperty("gpsDate")]
    private int[]? GpsDateArray { get; set; }

    [JsonProperty("receivedAt")]
    private long[]? ReceivedAtArray { get; set; }

    // 👇 Convert to proper DateTime
    [JsonIgnore]
    public DateTime GpsDate =>
        GpsDateArray != null && GpsDateArray.Length >= 6
            ? new DateTime(
                GpsDateArray[0],
                GpsDateArray[1],
                GpsDateArray[2],
                GpsDateArray[3],
                GpsDateArray[4],
                GpsDateArray[5])
            : default;

    [JsonIgnore]
    public DateTime? ReceivedAt =>
        ReceivedAtArray != null && ReceivedAtArray.Length >= 6
            ? new DateTime(
                (int)ReceivedAtArray[0],
                (int)ReceivedAtArray[1],
                (int)ReceivedAtArray[2],
                (int)ReceivedAtArray[3],
                (int)ReceivedAtArray[4],
                (int)ReceivedAtArray[5])
            : null;

    public string Id { get; set; } = string.Empty;
    public string VehicleNo { get; set; } = string.Empty;

    // keep your other bool properties here...
}
}
