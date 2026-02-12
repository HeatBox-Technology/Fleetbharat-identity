using System;

namespace Application.DTOs
{
    /// <summary>
    /// DTO for SIM card details and status.
    /// Example: { "AccountId": 1, "Iccid": "8991123456789012345", "Msisdn": "9876543210", "StatusKey": "active" }
    /// </summary>
    public class SimDto
    {
        /// <summary>Unique SIM ID</summary>
        public long SimId { get; set; }
        /// <summary>Account ID (FK to mst_account)</summary>
        public long AccountId { get; set; }
        /// <summary>ICCID (unique SIM identifier)</summary>
        public string Iccid { get; set; } = string.Empty;
        /// <summary>MSISDN (phone number, optional)</summary>
        public string? Msisdn { get; set; }
        /// <summary>IMSI (optional)</summary>
        public string? Imsi { get; set; }
        /// <summary>Network provider ID (FK to lkp_network_provider)</summary>
        public long? NetworkProviderId { get; set; }
        /// <summary>Status key (e.g. 'active', 'inactive')</summary>
        public string StatusKey { get; set; } = "active";
        /// <summary>Activation timestamp</summary>
        public DateTime? ActivatedAt { get; set; }
        /// <summary>Expiry timestamp</summary>
        public DateTime? ExpiryAt { get; set; }
        /// <summary>Creation timestamp</summary>
        public DateTime CreatedAt { get; set; }
    }
}
