using System;

/// <summary>
/// DTO for vehicle details (number, VIN, type, etc.).
/// Example: { "AccountId": 1, "VehicleNumber": "MH12AB1234", "VehicleTypeId": 2, "Status": "Active" }
/// </summary>
public class VehicleDto
{
    /// <summary>Unique vehicle ID</summary>
    public int Id { get; set; }
    /// <summary>Account ID (FK to mst_account)</summary>
    public int AccountId { get; set; }
    /// <summary>Vehicle number (registration number)</summary>
    public string VehicleNumber { get; set; } = string.Empty;
    /// <summary>VIN or chassis number</summary>
    public string VinOrChassisNumber { get; set; } = string.Empty;
    /// <summary>Registration date</summary>
    public DateTime RegistrationDate { get; set; }
    /// <summary>Vehicle type ID (FK to mst_vehicle_type)</summary>
    public int VehicleTypeId { get; set; }
    /// <summary>Vehicle brand OEM ID (FK to VehicleBrandOem)</summary>
    public int VehicleBrandOemId { get; set; }
    /// <summary>Ownership type (e.g. 'Owned', 'Leased')</summary>
    public string OwnershipType { get; set; } = string.Empty;
    /// <summary>Leased vendor ID (FK to LeasedVendor, optional)</summary>
    public int? LeasedVendorId { get; set; }
    /// <summary>Image file path (optional)</summary>
    public string? ImageFilePath { get; set; }
    /// <summary>Status (e.g. 'Active', 'Inactive')</summary>
    public string Status { get; set; } = "Active";
    // Optional fields
    /// <summary>Vehicle class (optional)</summary>
    public string? VehicleClass { get; set; }
    /// <summary>RTO passing (optional)</summary>
    public string? RtoPassing { get; set; }
    /// <summary>Warranty (optional)</summary>
    public string? Warranty { get; set; }
    /// <summary>Insurer (optional)</summary>
    public string? Insurer { get; set; }
    /// <summary>Vehicle color (optional)</summary>
    public string? VehicleColor { get; set; }
}
public class VehicleSummaryDto
{
    public int TotalFleetSize { get; set; }
    public int InService { get; set; }
    public int OutOfService { get; set; }
}
public class VehicleListUiResponseDto
{
    public VehicleSummaryDto Summary { get; set; } = new();

    public PagedResultDto<VehicleDto> Vehicles { get; set; } = new();
}
