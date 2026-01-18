using System;

public class mst_category
{
    public int CategoryId { get; set; }               // PK

    public string LabelName { get; set; } = "";       // Platinum Partner
    public string? Description { get; set; }          // Optional
    public bool IsActive { get; set; } = true;        // toggle

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public int? DeletedBy { get; set; }
}
