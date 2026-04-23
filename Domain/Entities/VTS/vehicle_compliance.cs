using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    [Table("vehicle_compliance")]
    public class vehicle_compliance : IAccountEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int AccountId { get; set; }

        public int VehicleId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ComplianceType { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string DocumentNumber { get; set; } = string.Empty;

        public DateTime IssueDate { get; set; }

        public DateTime ExpiryDate { get; set; }

        public int ReminderBeforeDays { get; set; } = 7;

        [MaxLength(500)]
        public string? DocumentPath { get; set; }

        [MaxLength(255)]
        public string? DocumentFileName { get; set; }

        [MaxLength(100)]
        public string? DocumentContentType { get; set; }

        public string? Remarks { get; set; }

        public int CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? UpdatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
