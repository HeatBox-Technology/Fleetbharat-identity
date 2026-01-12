using System;

public class map_FormRole_right
{
    public int RoleFormRightId { get; set; }

    public int RoleId { get; set; }

    public int FormId { get; set; }

    public bool CanRead { get; set; }

    public bool CanWrite { get; set; }

    public bool CanDelete { get; set; }

    public bool CanExport { get; set; }

    public bool CanAll { get; set; }
}
