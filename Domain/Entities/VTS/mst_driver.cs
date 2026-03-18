using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    /// <summary>
    /// Driver master table.
    /// </summary>
    [Table("mst_driver")]
    public class mst_driver : IAccountEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DriverId { get; set; }

        [Required]
        public int AccountId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        public string Mobile { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string LicenseNumber { get; set; } = string.Empty;

        public DateTime? LicenseExpiry { get; set; }

        [MaxLength(5)]
        public string? BloodGroup { get; set; }

        [MaxLength(15)]
        public string? EmergencyContact { get; set; }

        [Required]
        [MaxLength(20)]
        public string StatusKey { get; set; } = "active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int? UpdatedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        public bool IsDeleted { get; set; } = false;

        public bool IsActive { get; set; } = true;
    }
}
