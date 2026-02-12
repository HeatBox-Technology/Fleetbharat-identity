using System;

namespace Application.DTOs
{
    /// <summary>
    /// DTO for mapping a device to a vehicle, including installation position, history, and remarks.
    /// Example: { "AccountId": 1, "VehicleId": 101, "DeviceId": 55, "InstallPosition": "primary_gps", "IsPrimary": true }
    /// </summary>
    public class VehicleDeviceMapDto
    {
        /// <summary>Unique mapping ID</summary>
        public long VehicleDeviceId { get; set; }
        /// <summary>Account ID (FK to mst_account)</summary>
        public long AccountId { get; set; }
        /// <summary>Vehicle ID (FK to mst_vehicle)</summary>
        public long VehicleId { get; set; }
        /// <summary>Device ID (FK to mst_device)</summary>
        public long DeviceId { get; set; }
        /// <summary>Installation position (e.g. 'primary_gps', 'adas', 'fuel', etc.)</summary>
        public string InstallPosition { get; set; } = "primary_gps";
        /// <summary>Is this the primary device for the vehicle?</summary>
        public bool IsPrimary { get; set; } = false;
        /// <summary>Mapping start timestamp</summary>
        public DateTime FromTs { get; set; }
        /// <summary>Mapping end timestamp (null = active)</summary>
        public DateTime? ToTs { get; set; }
        /// <summary>User ID who installed the device (FK to mst_user)</summary>
        public long? InstalledByUserId { get; set; }
        /// <summary>Optional remarks about the installation</summary>
        public string? Remarks { get; set; }
    }
}
