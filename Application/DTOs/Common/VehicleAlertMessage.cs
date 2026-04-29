using System;

public class VehicleAlertMessage
{
    public int OrgId { get; set; }
    public string VehicleId { get; set; } = string.Empty;
    public string VehicleNo { get; set; } = string.Empty;
    public string DeviceNo { get; set; } = string.Empty;
    public string Imei { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Address { get; set; } = string.Empty;
    public DateTime? GpsDate { get; set; }
    public DateTime? ReceivedTime { get; set; }
    public string Severity { get; set; } = string.Empty;
}
