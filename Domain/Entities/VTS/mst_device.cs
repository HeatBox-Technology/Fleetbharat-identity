using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    /// <summary>
    /// Device master table, stores device details (IMEI, type, SIM, etc.).
    /// Example: A GPS device with IMEI '123456789012345' assigned to an account.
    /// </summary>
    [Table("mst_device")]
    public class mst_device
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Account ID (FK to mst_account)</summary>
        public int AccountId { get; set; }

        /// <summary>Device IMEI or serial number (unique)</summary>
        [Required]
        [MaxLength(50)]
        public string DeviceImeiOrSerial { get; set; } = string.Empty;

        /// <summary>Device type ID (FK to mst_device_type)</summary>
        public int DeviceTypeId { get; set; }

        /// <summary>Firmware version (optional)</summary>
        [MaxLength(50)]
        public string? FirmwareVersion { get; set; }

        /// <summary>SIM mobile number (optional)</summary>
        [MaxLength(20)]
        public string? SimMobile { get; set; }

        /// <summary>SIM ICCID (optional)</summary>
        [MaxLength(30)]
        public string? SimIccid { get; set; }

        /// <summary>Network provider ID (FK to lkp_network_provider, optional)</summary>
        public int? NetworkProviderId { get; set; }
        public NetworkProvider? NetworkProvider { get; set; }

        /// <summary>Device status (e.g. 'Active', 'Inactive')</summary>
        [MaxLength(20)]
        public string DeviceStatus { get; set; } = "Active";
    }
}
