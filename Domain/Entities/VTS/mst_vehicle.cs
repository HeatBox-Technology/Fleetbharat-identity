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

        /// <summary>Registration date</summary>
        public DateTime RegistrationDate { get; set; }

        /// <summary>Vehicle type ID (FK to mst_vehicle_type)</summary>
        public int VehicleTypeId { get; set; }

        /// <summary>Vehicle brand OEM ID (FK to VehicleBrandOem)</summary>
        public int VehicleBrandOemId { get; set; }
        public VehicleBrandOem? VehicleBrandOem { get; set; }

        /// <summary>Ownership type (e.g. 'Owned', 'Leased')</summary>
        [MaxLength(20)]
        public string OwnershipType { get; set; } = string.Empty;

        /// <summary>Leased vendor ID (FK to LeasedVendor, optional)</summary>
        public int? LeasedVendorId { get; set; }
        public LeasedVendor? LeasedVendor { get; set; }

        /// <summary>Image file path (optional)</summary>
        public string? ImageFilePath { get; set; }

        /// <summary>Status (e.g. 'Active', 'Inactive')</summary>
        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        // Optional fields
        /// <summary>Vehicle class (optional)</summary>
        public string? VehicleClass { get; set; }
        /// <summary>RTO passing (optional)</summary>
        public string? RtoPassing { get; set; }
        /// <summary>Warranty (optional)</summary>
        public string? Warranty { get; set; }
        /// <summary>Insurer (optional)</summary>
        public string? Insurer { get; set; }
        /// <summary>Vehicle color (optional)</summary>
        public string? VehicleColor { get; set; }
    }
}
