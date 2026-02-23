using System;


/// <summary>
/// DTO for SIM card details and status.
/// Example: { "AccountId": 1, "Iccid": "8991123456789012345", "Msisdn": "9876543210", "StatusKey": "active" }
/// </summary>
public class SimDto
{
    public int SimId { get; set; }

    public int AccountId { get; set; }

    public string Iccid { get; set; } = string.Empty;

    public string? Msisdn { get; set; }

    public string? Imsi { get; set; }

    public int? NetworkProviderId { get; set; }

    public string StatusKey { get; set; } = "active";

    public DateTime? ActivatedAt { get; set; }

    public DateTime? ExpiryAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }
}
public class CreateSimDto
{
    public int AccountId { get; set; }

    public string Iccid { get; set; } = string.Empty;

    public string? Msisdn { get; set; }

    public string? Imsi { get; set; }

    public int? NetworkProviderId { get; set; }

    public string StatusKey { get; set; } = "active";

    public DateTime? ActivatedAt { get; set; }

    public DateTime? ExpiryAt { get; set; }

    public int CreatedBy { get; set; }
}
public class UpdateSimDto
{
    public string Iccid { get; set; } = string.Empty;

    public string? Msisdn { get; set; }

    public string? Imsi { get; set; }

    public int? NetworkProviderId { get; set; }

    public string StatusKey { get; set; } = "active";

    public DateTime? ActivatedAt { get; set; }

    public DateTime? ExpiryAt { get; set; }

    public bool IsActive { get; set; }

    public int? UpdatedBy { get; set; }
}
public class SimSummaryDto
{
    public int TotalSims { get; set; }

    public int Active { get; set; }

    public int Inactive { get; set; }

    public int Expired { get; set; }
}
public class SimListUiResponseDto
{
    public SimSummaryDto Summary { get; set; } = new();

    public PagedResultDto<SimDto> Sims { get; set; } = new();
}

