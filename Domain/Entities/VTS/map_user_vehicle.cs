using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    /// <summary>
    /// Maps a user to a vehicle, for vehicle-wise login and filtering.
    /// Example: A user assigned access to a specific vehicle.
    /// </summary>
    [Table("map_user_vehicle")]
    public class map_user_vehicle
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserVehicleId { get; set; }

        /// <summary>
        /// User ID (FK to mst_user)
        /// </summary>
        [Required]
        public long UserId { get; set; }
        // public mst_user? User { get; set; } // Uncomment if mst_user entity exists

        /// <summary>
        /// Vehicle ID (FK to mst_vehicle)
        /// </summary>
        [Required]
        public int VehicleId { get; set; }
        public mst_vehicle? Vehicle { get; set; }

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
