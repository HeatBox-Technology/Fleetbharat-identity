using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("fr_mst_form_builder")]
public class FormBuilder
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int pk_form_builder_id { get; set; }

    public int? AccountId { get; set; }
    public int? FkFormId { get; set; }

    public string? FormTitle { get; set; }
    public string? FormCode { get; set; }
    public string? Description { get; set; }

    // Save complete builder JSON:
    // layout, fields, validation, order, properties, etc.
    public string? RawData { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    public Guid CreatedByUser { get; set; }
    public DateTime CreatedDate { get; set; }

    public Guid? UpdatedByUser { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public Guid? DeletedByUser { get; set; }
    public DateTime? DeletedDate { get; set; }

    // Optional display/reference columns
    public string? ProjectName { get; set; }
    public string? AccountName { get; set; }
    public string? FormName { get; set; }
}