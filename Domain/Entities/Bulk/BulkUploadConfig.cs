using System;

public class BulkUploadConfig
{
    public int Id { get; set; }
    public string ModuleKey { get; set; } = "";
    public string DtoName { get; set; } = "";
    public string ServiceInterface { get; set; } = "";
    public string ServiceMethod { get; set; } = "";
    public string ColumnsJson { get; set; } = "[]";
    public bool ExternalSync { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
