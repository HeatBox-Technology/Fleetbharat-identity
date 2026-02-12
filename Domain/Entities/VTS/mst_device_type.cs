using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    [Table("mst_device_type")]
    public class mst_device_type
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        [MaxLength(50)]
        public string DeviceType { get; set; } = string.Empty;
        public int OemManufacturerId { get; set; }
        public OemManufacturer? OemManufacturer { get; set; }
        public int DeviceCategoryId { get; set; }
        public DeviceCategory? DeviceCategory { get; set; }
        public int InputCount { get; set; }
        public int OutputCount { get; set; }
        public string? SupportedIOs { get; set; } // Comma-separated
        public string? SupportedFeatures { get; set; } // Comma-separated
        [MaxLength(20)]
        public string Status { get; set; } = "Active";
    }
}
