using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("form_fields")]
public class FormField
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string PageKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FieldKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string FieldLabel { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FieldType { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
