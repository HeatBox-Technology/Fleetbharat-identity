using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    /// <summary>
    /// Device master table, stores device details (IMEI, type, manufacturer, etc.).
    /// </summary>
    [Table("mst_device")]
    public class mst_device
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Account ID (FK to mst_account)</summary>
        public int AccountId { get; set; }

        /// <summary>Manufacturer ID (FK to mst_manufacturer)</summary>
        public int ManufactureID { get; set; }

        /// <summary>Device type ID (FK to mst_device_type)</summary>
        public int DeviceTypeId { get; set; }

        /// <summary>Internal device number</summary>
        [Required]
        [MaxLength(50)]
        public string DeviceNo { get; set; } = string.Empty;

        /// <summary>Device IMEI or serial number (unique)</summary>
        [Required]
        [MaxLength(50)]
        public string DeviceImeiOrSerial { get; set; } = string.Empty;

        /// <summary>Device status (Active, InService, OutOfService, etc.)</summary>
        [MaxLength(20)]
        public string DeviceStatus { get; set; } = "Active";

        /// <summary>Created by user</summary>
        public int createdBy { get; set; }

        /// <summary>Created timestamp</summary>
        public DateTime createdAt { get; set; } = DateTime.UtcNow;

        /// <summary>Updated by user</summary>
        public int? updatedBy { get; set; }

        /// <summary>Updated timestamp</summary>
        public DateTime? updatedAt { get; set; }

        /// <summary>Soft delete flag</summary>
        public bool IsDeleted { get; set; } = false;
    }
}