using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("account_form_configurations")]
public class AccountFormConfiguration : IAccountEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int AccountId { get; set; }

    [Required]
    [MaxLength(100)]
    public string PageKey { get; set; } = string.Empty;

    public int FieldId { get; set; }

    public bool Visible { get; set; }

    public bool Required { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
