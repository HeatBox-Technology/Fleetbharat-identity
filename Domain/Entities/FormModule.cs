using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
[Table("mst_form_module")]
public class FormModule
{
    public int FormModuleId { get; set; }
    public int? SolutionId { get; set; }

    public string ModuleCode { get; set; } = null!;

    public string ModuleName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public int? UpdatedBy { get; set; }

    public SolutionMaster? Solution { get; set; }
    public ICollection<mst_form> Forms { get; set; } = new List<mst_form>();
}
