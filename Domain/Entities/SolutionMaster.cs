using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

[Table("mst_solution_master")]
public class SolutionMaster
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<FormModule> Modules { get; set; } = new List<FormModule>();
}
