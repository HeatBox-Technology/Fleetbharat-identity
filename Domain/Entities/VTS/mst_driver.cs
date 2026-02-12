using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    /// <summary>
    /// Represents a driver in the system.
    /// </summary>
    public class mst_driver
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        [Key]
        public long DriverId { get; set; }

        /// <summary>
        /// Account to which the driver belongs.
        /// </summary>
        [Required]
        public long AccountId { get; set; }

        /// <summary>
        /// Driver's full name.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Driver's mobile number.
        /// </summary>
        [Required]
        [MaxLength(15)]
        public string Mobile { get; set; } = string.Empty;

        /// <summary>
        /// Driver's license number.
        /// </summary>
        [Required]
        [MaxLength(30)]
        public string LicenseNumber { get; set; } = string.Empty;

        /// <summary>
        /// License expiry date.
        /// </summary>
        public DateTime? LicenseExpiry { get; set; }

        /// <summary>
        /// Blood group of the driver.
        /// </summary>
        [MaxLength(5)]
        public string? BloodGroup { get; set; }

        /// <summary>
        /// Emergency contact number.
        /// </summary>
        [MaxLength(15)]
        public string? EmergencyContact { get; set; }

        /// <summary>
        /// Status key (active/inactive).
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string StatusKey { get; set; } = string.Empty;

        /// <summary>
        /// Created at timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
