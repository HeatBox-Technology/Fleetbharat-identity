using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    [Table("mst_vehicle_type")]
    public class mst_vehicle_type
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        [MaxLength(50)]
        public string VehicleTypeName { get; set; } = string.Empty;
        [MaxLength(30)]
        public string Category { get; set; } = string.Empty;
        public string? DefaultVehicleIcon { get; set; }
        public string? DefaultAlarmIcon { get; set; }
        public string? DefaultIconColor { get; set; }
        public int SeatingCapacity { get; set; }
        public int WheelsCount { get; set; }
        [MaxLength(20)]
        public string FuelCategory { get; set; } = string.Empty;
        public string? TankCapacity { get; set; }
        public string? DefaultSpeedLimit { get; set; }
        public string? DefaultIdleThreshold { get; set; }
        [MaxLength(20)]
        public string Status { get; set; } = "Active";
    }
}
