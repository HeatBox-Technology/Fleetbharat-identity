using System;

public class OemManufacturerDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? OfficialWebsite { get; set; }
    public string? OriginCountry { get; set; }
    public string? SupportEmail { get; set; }
    public string? SupportHotline { get; set; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
}
public class OemSummaryDto
{
    public int TotalEntities { get; set; }
    public int Enabled { get; set; }
    public int Disabled { get; set; }
}
public class OemListUiResponseDto
{
    public OemSummaryDto Summary { get; set; } = new();

    public PagedResultDto<OemManufacturerDto> Manufacturers { get; set; } = new();
}