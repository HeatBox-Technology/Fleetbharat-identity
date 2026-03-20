using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    /// <summary>
    /// Lookup table for sensor types (e.g. fuel, temperature, TPMS, etc.).
    /// Example: { "Code": "FUEL_LEVEL", "Name": "Fuel Level Sensor", "Unit": "L", "ValueKind": "number" }
    /// </summary>
    [Table("lkp_sensor_type")]
    public class lkp_sensor_type
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SensorTypeId { get; set; }

        /// <summary>Sensor type code (e.g. 'FUEL_LEVEL', 'TEMP')</summary>
        [Required]
        public string Code { get; set; } = string.Empty;

        /// <summary>Sensor type name (user-friendly)</summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>Unit of measurement (e.g. 'L', '°C', '%')</summary>
        public string? Unit { get; set; }

        /// <summary>Value kind (number, boolean, string)</summary>
        [Required]
        public string ValueKind { get; set; } = "number";

        /// <summary>Minimum value (optional)</summary>
        public decimal? MinValue { get; set; }
        /// <summary>Maximum value (optional)</summary>
        public decimal? MaxValue { get; set; }

        /// <summary>Is this sensor type active?</summary>
        [Required]
        public bool IsActive { get; set; } = true;
    }
}
