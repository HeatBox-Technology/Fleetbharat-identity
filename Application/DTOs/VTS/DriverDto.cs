using System;

namespace Application.DTOs
{
    /// <summary>
    /// Data Transfer Object for Driver.
    /// </summary>
    public class DriverDto
    {
        /// <example>1</example>
        public long DriverId { get; set; }
        /// <example>101</example>
        public long AccountId { get; set; }
        /// <example>John Doe</example>
        public string Name { get; set; } = string.Empty;
        /// <example>9876543210</example>
        public string Mobile { get; set; } = string.Empty;
        /// <example>MH1234567890</example>
        public string LicenseNumber { get; set; } = string.Empty;
        /// <example>2027-01-01</example>
        public DateTime? LicenseExpiry { get; set; }
        /// <example>O+</example>
        public string BloodGroup { get; set; } = string.Empty;
        /// <example>9123456789</example>
        public string EmergencyContact { get; set; } = string.Empty;
        /// <example>active</example>
        public string StatusKey { get; set; } = string.Empty;
        /// <example>2026-01-31T12:00:00Z</example>
        public DateTime CreatedAt { get; set; }
    }
}
