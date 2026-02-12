using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    /// <summary>
    /// Maps a device to a vehicle, including installation position, history, and remarks.
    /// Example: A GPS device installed as 'primary_gps' on a vehicle.
    /// </summary>
    [Table("map_vehicle_device")]
    public class map_vehicle_device
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long VehicleDeviceId { get; set; }

        /// <summary>
        /// Account ID (FK to mst_account)
        /// </summary>
        [Required]
        public long AccountId { get; set; }

        /// <summary>
        /// Vehicle ID (FK to mst_vehicle)
        /// </summary>
        [Required]
        public long VehicleId { get; set; }
        public mst_vehicle? Vehicle { get; set; }

        /// <summary>
        /// Device ID (FK to mst_device)
        /// </summary>
        [Required]
        public long DeviceId { get; set; }
        public mst_device? Device { get; set; }

        /// <summary>
        /// Installation position (e.g. 'primary_gps', 'adas', 'fuel', etc.)
        /// </summary>
        [Required]
        public string InstallPosition { get; set; } = "primary_gps";

        /// <summary>
        /// Is this the primary device for the vehicle?
        /// </summary>
        [Required]
        public bool IsPrimary { get; set; } = false;

        /// <summary>
        /// Mapping start timestamp
        /// </summary>
        [Required]
        public DateTime FromTs { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Mapping end timestamp (null = active)
        /// </summary>
        public DateTime? ToTs { get; set; }

        /// <summary>
        /// User ID who installed the device (FK to mst_user)
        /// </summary>
        public long? InstalledByUserId { get; set; }
        // public mst_user? InstalledByUser { get; set; } // Uncomment if mst_user entity exists

        /// <summary>
        /// Optional remarks about the installation
        /// </summary>
        public string? Remarks { get; set; }
    }
}
