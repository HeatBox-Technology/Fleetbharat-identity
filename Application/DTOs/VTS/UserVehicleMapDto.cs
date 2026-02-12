using System;

namespace Application.DTOs
{
    /// <summary>
    /// DTO for mapping a user to a vehicle, for vehicle-wise login and filtering.
    /// Example: { "UserId": 10, "VehicleId": 101, "FromTs": "2024-01-01T00:00:00Z" }
    /// </summary>
    public class UserVehicleMapDto
    {
        /// <summary>Unique mapping ID</summary>
        public long UserVehicleId { get; set; }
        /// <summary>User ID (FK to mst_user)</summary>
        public long UserId { get; set; }
        /// <summary>Vehicle ID (FK to mst_vehicle)</summary>
        public long VehicleId { get; set; }
        /// <summary>Mapping start timestamp</summary>
        public DateTime FromTs { get; set; }
        /// <summary>Mapping end timestamp (null = active)</summary>
        public DateTime? ToTs { get; set; }
    }
}
