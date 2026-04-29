using System;

namespace Application.DTOs
{
    /// <summary>
    /// DTO for mapping a vehicle to a sensor with mount point and history.
    /// Example: { VehicleId = 101, SensorId = 5, MountPoint = "tank1" }
    /// </summary>
    public class VehicleSensorMapDto
    {
        public long VehicleSensorId { get; set; }
        public int VehicleId { get; set; }
        public long SensorId { get; set; }
        public string MountPoint { get; set; } = "default";
        public DateTime FromTs { get; set; }
        public DateTime? ToTs { get; set; }
    }
}
