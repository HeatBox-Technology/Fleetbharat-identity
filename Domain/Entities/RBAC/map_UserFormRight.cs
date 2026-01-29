using System;

public class map_UserFormRight
{
    public int UserFormRightId { get; set; }
    public Guid UserId { get; set; }
    public int AccountId { get; set; }
    public int FormId { get; set; }

    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }
    public bool CanAll { get; set; }
}