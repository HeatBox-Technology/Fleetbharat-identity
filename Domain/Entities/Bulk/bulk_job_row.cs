public class bulk_job_row
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public string ModuleName { get; set; } = "";

    public int RowNumber { get; set; }

    public string PayloadJson { get; set; } = "";

    public string Status { get; set; } = "PENDING";

    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }
}