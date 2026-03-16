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
    public class map_vehicle_device : IAccountEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Account ID (FK to mst_account)
        /// </summary>
        [Required]
        public int AccountId { get; set; }

        /// <summary>
        /// Vehicle ID (FK to mst_vehicle)
        /// </summary>
        [Required]
        public int Fk_VehicleId { get; set; }

        public int fk_devicetypeid { get; set; }

        [Required]
        public int Fk_DeviceId { get; set; }

        public int fk_simid { get; set; }
        public string simnno { get; set; } = string.Empty;
        [Required]
        public string? Remarks { get; set; }
        public bool IsActive { get; set; } = true; // true for active, false for inactive
        public bool IsDeleted { get; set; } = false; // false for not deleted, true for deleted
        public DateTime InstallationDate { get; set; } = DateTime.UtcNow;
        public int createdBy { get; set; }
        public DateTime createdAt { get; set; } = DateTime.UtcNow;
        public int? updatedBy { get; set; }
        public DateTime? updatedAt { get; set; }
        [ForeignKey("Fk_VehicleId")]
        public mst_vehicle? Vehicle { get; set; }

        [ForeignKey("Fk_DeviceId")]
        public mst_device? Device { get; set; }
    }
}
