using System;

public class BulkUploadStatusDto
{
    public int JobId { get; set; }
    public string ModuleKey { get; set; } = "";
    public string Status { get; set; } = "PENDING";
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int SuccessRows { get; set; }
    public int FailedRows { get; set; }
    public string? ErrorFilePath { get; set; }
    public DateTime? CompletedAt { get; set; }
}
