using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    /// <summary>
    /// SIM master table, stores SIM card details and status.
    /// Example: A Jio SIM with ICCID '8991...1234' assigned to an account.
    /// </summary>
    [Table("mst_sim")]
    public class mst_sim
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SimId { get; set; }

        /// <summary>Account ID (FK to mst_account)</summary>
        [Required]
        public long AccountId { get; set; }

        /// <summary>ICCID (unique SIM identifier)</summary>
        [Required]
        public string Iccid { get; set; } = string.Empty;

        /// <summary>MSISDN (phone number, optional)</summary>
        public string? Msisdn { get; set; }

        /// <summary>IMSI (optional)</summary>
        public string? Imsi { get; set; }

        /// <summary>Network provider ID (FK to lkp_network_provider)</summary>
        public long? NetworkProviderId { get; set; }
        public NetworkProvider? NetworkProvider { get; set; }

        /// <summary>Status key (e.g. 'active', 'inactive')</summary>
        [Required]
        [MaxLength(20)]
        public string StatusKey { get; set; } = "active";

        /// <summary>Activation timestamp</summary>
        public DateTime? ActivatedAt { get; set; }
        /// <summary>Expiry timestamp</summary>
        public DateTime? ExpiryAt { get; set; }

        /// <summary>Creation timestamp</summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}