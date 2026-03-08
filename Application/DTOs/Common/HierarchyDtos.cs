using System.Collections.Generic;
using System.Text.Json;

public class SolutionListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}

public class ModuleListItemDto
{
    public int FormModuleId { get; set; }
    public int? SolutionId { get; set; }
    public string ModuleCode { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public string? Description { get; set; }
}

public class FormFilterConfigResponseDto
{
    public string FormName { get; set; } = "";
    public JsonElement Filters { get; set; }
}

