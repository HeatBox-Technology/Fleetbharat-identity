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
        /// <summary>Moving icon URL/path (optional)</summary>
        public string? MovingIcon { get; set; }
        /// <summary>Stopped icon URL/path (optional)</summary>
        public string? StoppedIcon { get; set; }
        /// <summary>Idle icon URL/path (optional)</summary>
        public string? IdleIcon { get; set; }
        /// <summary>Parked icon URL/path (optional)</summary>
        public string? ParkedIcon { get; set; }
        /// <summary>Offline icon URL/path (optional)</summary>
        public string? OfflineIcon { get; set; }
        /// <summary>Breakdown icon URL/path (optional)</summary>
        public string? BreakdownIcon { get; set; }
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
