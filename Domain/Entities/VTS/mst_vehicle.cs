using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    /// <summary>
    /// Vehicle master table, stores vehicle details (number, VIN, type, etc.).
    /// Example: A truck with vehicle number 'MH12AB1234' and VIN 'XYZ123456789'.
    /// </summary>
    [Table("mst_vehicle")]
    public class mst_vehicle
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Account ID (FK to mst_account)</summary>
        public int AccountId { get; set; }

        /// <summary>Vehicle number (registration number)</summary>
        [Required]
        [MaxLength(50)]
        public string VehicleNumber { get; set; } = string.Empty;

        /// <summary>VIN or chassis number</summary>
        [MaxLength(50)]
        public string VinOrChassisNumber { get; set; } = string.Empty;

        /// <summary>Vehicle type ID (FK to mst_vehicle_type)</summary>
        public int VehicleTypeId { get; set; }

        /// <summary>Vehicle brand OEM ID (FK to VehicleBrandOem)</summary>

        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

    }
}
