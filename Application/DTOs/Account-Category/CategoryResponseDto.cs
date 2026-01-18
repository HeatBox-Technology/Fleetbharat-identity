using System;

public class CategoryResponseDto
{
    public int CategoryId { get; set; }
    public string LabelName { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}