using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    [Table("device_transfer_items")]
    public class device_transfer_item
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int TransferId { get; set; }
        public device_transfer? Transfer { get; set; }

        public int DeviceId { get; set; }
        public mst_device? Device { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
