using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    [Table("map_driver_vehicle_assignment")]
    public class map_driver_vehicle_assignment : IAccountEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int AccountId { get; set; }

        public int DriverId { get; set; }

        public int VehicleId { get; set; }

        [Required]
        [MaxLength(50)]
        public string AssignmentLogic { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public DateTime? ExpectedEnd { get; set; }

        [MaxLength(1000)]
        public string? DispatcherNotes { get; set; }

        public int CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? UpdatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int? DeletedBy { get; set; }

        public DateTime? DeletedAt { get; set; }

        public Guid? CreatedByUser { get; set; }

        public DateTime? CreatedAtUser { get; set; }

        public Guid? UpdatedByUser { get; set; }

        public DateTime? UpdatedAtUser { get; set; }

        public Guid? DeletedByUser { get; set; }

        public DateTime? DeletedAtUser { get; set; }

        public bool IsDeleted { get; set; } = false;

        public mst_driver? Driver { get; set; }

        public mst_vehicle? Vehicle { get; set; }
    }
}
