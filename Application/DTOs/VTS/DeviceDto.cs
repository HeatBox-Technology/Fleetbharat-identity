using System;

namespace Application.DTOs
{
    /// <summary>
    /// DTO for device details.
    /// </summary>
    public class DeviceDto
    {
        public int Id { get; set; }

        /// <summary>Account ID</summary>
        public int AccountId { get; set; }

        /// <summary>Manufacturer ID</summary>
        public int ManufactureID { get; set; }

        /// <summary>Device type ID</summary>
        public int DeviceTypeId { get; set; }

        /// <summary>Internal device number</summary>
        public string DeviceNo { get; set; } = string.Empty;

        /// <summary>IMEI or Serial Number</summary>
        public string DeviceImeiOrSerial { get; set; } = string.Empty;

        /// <summary>Status (Active, InService, OutOfService)</summary>
        public string DeviceStatus { get; set; } = "Active";

        public int CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? UpdatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }
    }


    public class DeviceCardSummaryDto
    {
        public int TotalDevices { get; set; }

        public int InService { get; set; }

        public int OutOfService { get; set; }
    }


    public class DeviceListUiResponseDto
    {
        public DeviceCardSummaryDto Summary { get; set; } = new();

        public PagedResultDto<DeviceDto> Devices { get; set; } = new();
    }
}