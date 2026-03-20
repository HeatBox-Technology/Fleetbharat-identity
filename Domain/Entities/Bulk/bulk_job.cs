using System;

public class bulk_job
{
    public int Id { get; set; }
    public string ModuleName { get; set; } = "";
    public string? FileName { get; set; }

    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int SuccessRows { get; set; }
    public int FailedRows { get; set; }

    public string Status { get; set; } = "PENDING";

    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public string? ErrorFilePath { get; set; }
}