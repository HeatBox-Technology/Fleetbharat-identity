using System;

namespace Application.DTOs
{
    /// <summary>
    /// DTO for mapping a SIM to a device, with history of assignment.
    /// Example: { "DeviceId": 55, "SimId": 123, "FromTs": "2024-01-01T00:00:00Z" }
    /// </summary>
    public class DeviceSimMapDto
    {
        /// <summary>Unique mapping ID</summary>
        public long DeviceSimId { get; set; }
        /// <summary>Device ID (FK to mst_device)</summary>
        public long DeviceId { get; set; }
        /// <summary>SIM ID (FK to mst_sim)</summary>
        public long SimId { get; set; }
        /// <summary>Mapping start timestamp</summary>
        public DateTime FromTs { get; set; }
        /// <summary>Mapping end timestamp (null = active)</summary>
        public DateTime? ToTs { get; set; }
    }
}
