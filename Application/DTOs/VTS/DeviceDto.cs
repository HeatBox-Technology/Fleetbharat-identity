using System;

namespace Application.DTOs
{
    /// <summary>
    /// DTO for device details (IMEI, type, SIM, etc.).
    /// Example: { "AccountId": 1, "DeviceImeiOrSerial": "123456789012345", "DeviceTypeId": 2, "DeviceStatus": "Active" }
    /// </summary>
    public class DeviceDto
    {
        /// <summary>Unique device ID</summary>
        public int Id { get; set; }
        /// <summary>Account ID (FK to mst_account)</summary>
        public int AccountId { get; set; }
        /// <summary>Device IMEI or serial number (unique)</summary>
        public string DeviceImeiOrSerial { get; set; } = string.Empty;
        /// <summary>Device type ID (FK to mst_device_type)</summary>
        public int DeviceTypeId { get; set; }
        /// <summary>Firmware version (optional)</summary>
        public string? FirmwareVersion { get; set; }
        /// <summary>SIM mobile number (optional)</summary>
        public string? SimMobile { get; set; }
        /// <summary>SIM ICCID (optional)</summary>
        public string? SimIccid { get; set; }
        /// <summary>Network provider ID (FK to lkp_network_provider, optional)</summary>
        public int? NetworkProviderId { get; set; }
        /// <summary>Device status (e.g. 'Active', 'Inactive')</summary>
        public string DeviceStatus { get; set; } = "Active";
    }
}
