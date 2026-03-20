using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    /// <summary>
    /// Maps a vehicle to a sensor, with mount point and history.
    /// Example: A fuel sensor mounted at 'tank1' on a vehicle.
    /// </summary>
    [Table("map_vehicle_sensor")]
    public class map_vehicle_sensor
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long VehicleSensorId { get; set; }

        /// <summary>
        /// Vehicle ID (FK to mst_vehicle)
        /// </summary>
        [Required]
        public long VehicleId { get; set; }
        public mst_vehicle? Vehicle { get; set; }

        /// <summary>
        /// Sensor ID (FK to mst_sensor)
        /// </summary>
        [Required]
        public long SensorId { get; set; }
        public mst_sensor? Sensor { get; set; }

        /// <summary>
        /// Mount point (e.g. 'tank1', 'cargo', 'reefer', 'cabin', etc.)
        /// </summary>
        [Required]
        public string MountPoint { get; set; } = "default";

        /// <summary>
        /// Mapping start timestamp
        /// </summary>
        [Required]
        public DateTime FromTs { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Mapping end timestamp (null = active)
        /// </summary>
        public DateTime? ToTs { get; set; }
    }
}
