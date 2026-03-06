public class BulkUploadStartResultDto
{
    public int JobId { get; set; }
    public string ModuleKey { get; set; } = "";
    public int TotalRows { get; set; }
    public string Status { get; set; } = "PENDING";
}
