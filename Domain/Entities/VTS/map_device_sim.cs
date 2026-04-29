using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    /// <summary>
    /// Maps a SIM to a device, with history of assignment.
    /// Example: A SIM card assigned to a GPS device.
    /// </summary>
    [Table("map_device_sim")]
    public class map_device_sim
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long DeviceSimId { get; set; }

        /// <summary>
        /// Device ID (FK to mst_device)
        /// </summary>
        [Required]
        public int DeviceId { get; set; }
        public mst_device? Device { get; set; }

        /// <summary>
        /// SIM ID (FK to mst_sim)
        /// </summary>
        [Required]
        public int SimId { get; set; }
        public mst_sim? Sim { get; set; }

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
