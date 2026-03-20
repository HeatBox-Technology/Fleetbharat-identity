using System;


public class DriverDto
{
    public int DriverId { get; set; }

    public int AccountId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Mobile { get; set; } = string.Empty;

    public string LicenseNumber { get; set; } = string.Empty;

    public DateTime? LicenseExpiry { get; set; }

    public string? BloodGroup { get; set; }

    public string? EmergencyContact { get; set; }

    public string StatusKey { get; set; } = "active";

    public DateTime CreatedAt { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }
}
public class CreateDriverDto
{
    public int AccountId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Mobile { get; set; } = string.Empty;

    public string LicenseNumber { get; set; } = string.Empty;

    public DateTime? LicenseExpiry { get; set; }

    public string? BloodGroup { get; set; }

    public string? EmergencyContact { get; set; }

    public string StatusKey { get; set; } = "active";

    public int CreatedBy { get; set; }
}
public class UpdateDriverDto
{
    public string Name { get; set; } = string.Empty;

    public string Mobile { get; set; } = string.Empty;

    public string LicenseNumber { get; set; } = string.Empty;

    public DateTime? LicenseExpiry { get; set; }

    public string? BloodGroup { get; set; }

    public string? EmergencyContact { get; set; }

    public string StatusKey { get; set; } = "active";

    public bool IsActive { get; set; }

    public int? UpdatedBy { get; set; }
}
public class DriverSummaryDto
{
    public int TotalDrivers { get; set; }

    public int Active { get; set; }

    public int Inactive { get; set; }

    public int LicenseExpiringSoon { get; set; }
}
public class DriverListUiResponseDto
{
    public DriverSummaryDto Summary { get; set; } = new();

    public PagedResultDto<DriverDto> Drivers { get; set; } = new();
}
