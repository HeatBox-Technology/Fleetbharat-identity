using System.Collections.Generic;

namespace Application.DTOs
{
    /// <summary>
    /// DTO for device type details (e.g. GPS, OBD, E-Lock).
    /// Example: { "DeviceType": "GPS", "OemManufacturerId": 1, "DeviceCategoryId": 2, "SupportedFeatures": ["GPS", "CAN"] }
    /// </summary>
    public class DeviceTypeDto
    {
        /// <summary>Unique device type ID</summary>
        public int Id { get; set; }
        /// <summary>Device type name (e.g. 'GPS', 'OBD')</summary>
        public string DeviceType { get; set; } = string.Empty;
        /// <summary>OEM manufacturer ID (FK to OemManufacturer)</summary>
        public int OemManufacturerId { get; set; }
        /// <summary>Device category ID (FK to DeviceCategory)</summary>
        public int DeviceCategoryId { get; set; }
        /// <summary>Number of input channels</summary>
        public int InputCount { get; set; }
        /// <summary>Number of output channels</summary>
        public int OutputCount { get; set; }
        /// <summary>Supported IOs (optional, e.g. ["IGN", "DOOR"])</summary>
        public List<string>? SupportedIOs { get; set; }
        /// <summary>Supported features (optional, e.g. ["GPS", "CAN"])</summary>
        public List<string>? SupportedFeatures { get; set; }
        /// <summary>Status (e.g. 'Active', 'Inactive')</summary>
        public string Status { get; set; } = "Active";
    }
}
