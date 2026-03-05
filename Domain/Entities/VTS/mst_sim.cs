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
public class mst_sim : IAccountEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SimId { get; set; }

        /// <summary>Account ID (FK to mst_account)</summary>
        [Required]
        public int AccountId { get; set; }

        /// <summary>ICCID (unique SIM identifier)</summary>
        [Required]
        [MaxLength(50)]
        public string Iccid { get; set; } = string.Empty;

        /// <summary>MSISDN (phone number)</summary>
        [MaxLength(20)]
        public string? Msisdn { get; set; }

        /// <summary>IMSI</summary>
        [MaxLength(30)]
        public string? Imsi { get; set; }

        /// <summary>Network provider ID (FK to lkp_network_provider)</summary>
        public int? NetworkProviderId { get; set; }

        [ForeignKey(nameof(NetworkProviderId))]
        public NetworkProvider? NetworkProvider { get; set; }

        /// <summary>Status key (active, inactive, suspended)</summary>
        [Required]
        [MaxLength(20)]
        public string StatusKey { get; set; } = "active";

        /// <summary>Activation timestamp</summary>
        public DateTime? ActivatedAt { get; set; }

        /// <summary>Expiry timestamp</summary>
        public DateTime? ExpiryAt { get; set; }

        /// <summary>Audit fields</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }
}
