using System;
using System.ComponentModel.DataAnnotations;

internal static class VehicleValidationRules
{
    public const string VinPattern = "^[A-HJ-NPR-Z0-9]{17}$";
}

/// <summary>
/// DTO for vehicle details (number, VIN, type, etc.)
/// </summary>
public class VehicleDto
{
    /// <summary>Unique vehicle ID</summary>
    public int Id { get; set; }

    /// <summary>Account ID (FK to mst_account)</summary>
    public int AccountId { get; set; }

    /// <summary>Vehicle number (registration number)</summary>
    [Required(ErrorMessage = "Vehicle number is required.")]
    [StringLength(20, ErrorMessage = "Vehicle number cannot exceed 20 characters.")]
    public string VehicleNumber { get; set; } = string.Empty;

    /// <summary>VIN or chassis number</summary>
    [Required(ErrorMessage = "VIN/Chassis number is required.")]
    [StringLength(17, ErrorMessage = "VIN/Chassis number must be exactly 17 characters.")]
    public string VinOrChassisNumber { get; set; } = string.Empty;

    /// <summary>Vehicle type ID (FK to mst_vehicle_type)</summary>
    public int VehicleTypeId { get; set; }

    /// <summary>Status (Active / Inactive)</summary>
    public string Status { get; set; } = "Active";

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
}
public class CreateVehicleDto
{
    public int AccountId { get; set; }

    [Required(ErrorMessage = "Vehicle number is required.")]
    [StringLength(20, ErrorMessage = "Vehicle number cannot exceed 20 characters.")]
    public string VehicleNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "VIN/Chassis number is required.")]
    [StringLength(17, ErrorMessage = "VIN/Chassis number must be exactly 17 characters.")]
    public string VinOrChassisNumber { get; set; } = string.Empty;

    public int VehicleTypeId { get; set; }
    public int CreatedBy { get; set; }
}
public class UpdateVehicleDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vehicle number is required.")]
    [StringLength(20, ErrorMessage = "Vehicle number cannot exceed 20 characters.")]
    public string VehicleNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "VIN/Chassis number is required.")]
    [StringLength(17, ErrorMessage = "VIN/Chassis number must be exactly 17 characters.")]
    public string VinOrChassisNumber { get; set; } = string.Empty;

    public int VehicleTypeId { get; set; }
    public int updatedBy { get; set; }
    public string Status { get; set; } = "Active";
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
