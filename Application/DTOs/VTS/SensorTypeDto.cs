namespace Application.DTOs
{
    /// <summary>
    /// DTO for sensor type lookup (e.g. fuel, temperature, TPMS, etc.).
    /// Example: { "Code": "FUEL_LEVEL", "Name": "Fuel Level Sensor", "Unit": "L", "ValueKind": "number" }
    /// </summary>
    public class SensorTypeDto
    {
        /// <summary>Unique sensor type ID</summary>
        public long SensorTypeId { get; set; }
        /// <summary>Sensor type code (e.g. 'FUEL_LEVEL', 'TEMP')</summary>
        public string Code { get; set; } = string.Empty;
        /// <summary>Sensor type name (user-friendly)</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Unit of measurement (e.g. 'L', '°C', '%')</summary>
        public string? Unit { get; set; }
        /// <summary>Value kind (number, boolean, string)</summary>
        public string ValueKind { get; set; } = "number";
        /// <summary>Minimum value (optional)</summary>
        public decimal? MinValue { get; set; }
        /// <summary>Maximum value (optional)</summary>
        public decimal? MaxValue { get; set; }
        /// <summary>Is this sensor type active?</summary>
        public bool IsActive { get; set; } = true;
    }
}
