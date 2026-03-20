using System;

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
    public string VehicleNumber { get; set; } = string.Empty;

    /// <summary>VIN or chassis number</summary>
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
    public string VehicleNumber { get; set; } = string.Empty;
    public string VinOrChassisNumber { get; set; } = string.Empty;
    public int VehicleTypeId { get; set; }
    public int CreatedBy { get; set; }
}
public class UpdateVehicleDto
{
    public int Id { get; set; }
    public string VehicleNumber { get; set; } = string.Empty;
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