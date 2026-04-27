using System;
using System.Collections.Generic;

public class CreateVehicleAccessRequest
{
    public int AccountId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int VehicleId { get; set; }
    public List<int> FormIds { get; set; } = new();
    public DateTime AccessStartDate { get; set; }
    public DateTime? AccessEndDate { get; set; }
    public bool CanViewTracking { get; set; }
    public bool CanViewReports { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class UpdateVehicleAccessRequest
{
    public string UserId { get; set; } = string.Empty;
    public int VehicleId { get; set; }
    public List<int> FormIds { get; set; } = new();
    public DateTime AccessStartDate { get; set; }
    public DateTime? AccessEndDate { get; set; }
    public bool CanViewTracking { get; set; }
    public bool CanViewReports { get; set; }
    public bool IsActive { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public class DeleteVehicleAccessRequest
{
    public string DeletedBy { get; set; } = string.Empty;
}

public class VehicleAccessFormResponse
{
    public int FormId { get; set; }
    public string FormName { get; set; } = string.Empty;
}

public class VehicleAccessResponse
{
    public long Id { get; set; }
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int VehicleId { get; set; }
    public string VehicleNumber { get; set; } = string.Empty;
    public List<int> FormIds { get; set; } = new();
    public List<VehicleAccessFormResponse> Forms { get; set; } = new();
    public DateTime AccessStartDate { get; set; }
    public DateTime? AccessEndDate { get; set; }
    public bool CanViewTracking { get; set; }
    public bool CanViewReports { get; set; }
    public bool IsActive { get; set; }
}

public class VehicleAccessListResponse
{
    public List<VehicleAccessResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class AccountUserOptionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}
