using System.Collections.Generic;

public class BulkUploadWorkItem
{
    public int JobId { get; set; }
    public string ModuleKey { get; set; } = "";
    public List<Dictionary<string, string>> Rows { get; set; } = new();
    public int? CreatedBy { get; set; }
}
