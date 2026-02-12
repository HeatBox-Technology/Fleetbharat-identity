using System.Collections.Generic;

namespace Application.DTOs
{
    /// <summary>
    /// DTO for vehicle type details (e.g. truck, car, bus).
    /// Example: { "VehicleTypeName": "Truck", "Category": "Heavy", "SeatingCapacity": 2, "WheelsCount": 6, "Status": "Active" }
    /// </summary>
    public class VehicleTypeDto
    {
        /// <summary>Unique vehicle type ID</summary>
        public int Id { get; set; }
        /// <summary>Vehicle type name (e.g. 'Truck', 'Car')</summary>
        public string VehicleTypeName { get; set; } = string.Empty;
        /// <summary>Category (e.g. 'Heavy', 'Light')</summary>
        public string Category { get; set; } = string.Empty;
        /// <summary>Default vehicle icon (optional)</summary>
        public string? DefaultVehicleIcon { get; set; }
        /// <summary>Default alarm icon (optional)</summary>
        public string? DefaultAlarmIcon { get; set; }
        /// <summary>Default icon color (optional)</summary>
        public string? DefaultIconColor { get; set; }
        /// <summary>Seating capacity</summary>
        public int SeatingCapacity { get; set; }
        /// <summary>Number of wheels</summary>
        public int WheelsCount { get; set; }
        /// <summary>Fuel category (e.g. 'Diesel', 'Petrol')</summary>
        public string FuelCategory { get; set; } = string.Empty;
        /// <summary>Tank capacity (optional)</summary>
        public string? TankCapacity { get; set; }
        /// <summary>Default speed limit (optional)</summary>
        public string? DefaultSpeedLimit { get; set; }
        /// <summary>Default idle threshold (optional)</summary>
        public string? DefaultIdleThreshold { get; set; }
        /// <summary>Status (e.g. 'Active', 'Inactive')</summary>
        public string Status { get; set; } = "Active";
    }
}
