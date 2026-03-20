using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    /// <summary>
    /// Sensor master table, stores physical/logical sensor details.
    /// Example: A fuel level sensor with serial number 'SN12345' for a specific account.
    /// </summary>
    [Table("mst_sensor")]
    public class mst_sensor
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SensorId { get; set; }

        /// <summary>Account ID (FK to mst_account)</summary>
        [Required]
        public long AccountId { get; set; }

        /// <summary>Sensor type ID (FK to lkp_sensor_type)</summary>
        [Required]
        public long SensorTypeId { get; set; }
        public lkp_sensor_type? SensorType { get; set; }

        /// <summary>Sensor name (user-friendly)</summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>Make/model (optional)</summary>
        public string? MakeModel { get; set; }
        /// <summary>Serial number (optional)</summary>
        public string? SerialNo { get; set; }

        /// <summary>Status key (e.g. 'active', 'inactive')</summary>
        [Required]
        [MaxLength(20)]
        public string StatusKey { get; set; } = "active";

        /// <summary>Creation timestamp</summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
