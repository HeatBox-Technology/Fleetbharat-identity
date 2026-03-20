using System;

namespace Application.DTOs
{
    /// <summary>
    /// DTO for sensor details (physical/logical sensor).
    /// Example: { "AccountId": 1, "SensorTypeId": 2, "Name": "Fuel Sensor 1", "SerialNo": "SN12345", "StatusKey": "active" }
    /// </summary>
    public class SensorDto
    {
        /// <summary>Unique sensor ID</summary>
        public long SensorId { get; set; }
        /// <summary>Account ID (FK to mst_account)</summary>
        public long AccountId { get; set; }
        /// <summary>Sensor type ID (FK to lkp_sensor_type)</summary>
        public long SensorTypeId { get; set; }
        /// <summary>Sensor name (user-friendly)</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Make/model (optional)</summary>
        public string? MakeModel { get; set; }
        /// <summary>Serial number (optional)</summary>
        public string? SerialNo { get; set; }
        /// <summary>Status key (e.g. 'active', 'inactive')</summary>
        public string StatusKey { get; set; } = "active";
        /// <summary>Creation timestamp</summary>
        public DateTime CreatedAt { get; set; }
    }
}
