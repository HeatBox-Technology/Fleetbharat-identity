using System;

public class NetworkProviderDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class NetworkProviderSummaryDto
{
    public int TotalEntities { get; set; }
    public int Enabled { get; set; }
    public int Disabled { get; set; }
}

public class NetworkProviderListUiResponseDto
{
    public NetworkProviderSummaryDto Summary { get; set; } = new();
    public PagedResultDto<NetworkProviderDto> Providers { get; set; } = new();
}
