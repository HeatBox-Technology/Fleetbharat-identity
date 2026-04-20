using System;
using Microsoft.AspNetCore.Http;

public class VehicleComplianceDto
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public int VehicleId { get; set; }
    public string VehicleNumber { get; set; } = string.Empty;
    public string ComplianceType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int ReminderBeforeDays { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? DocumentPath { get; set; }
    public string? DocumentFileName { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateVehicleComplianceDto
{
    public int AccountId { get; set; }
    public int VehicleId { get; set; }
    public string ComplianceType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int ReminderBeforeDays { get; set; } = 7;
    public string? DocumentPath { get; set; }
    public string? DocumentFileName { get; set; }
    public string? Remarks { get; set; }
    public int CreatedBy { get; set; }
}

public class UpdateVehicleComplianceDto
{
    public int AccountId { get; set; }
    public int VehicleId { get; set; }
    public string ComplianceType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int ReminderBeforeDays { get; set; } = 7;
    public string? DocumentPath { get; set; }
    public string? DocumentFileName { get; set; }
    public string? Remarks { get; set; }
    public int? UpdatedBy { get; set; }
}

public class VehicleComplianceFormDto : CreateVehicleComplianceDto
{
    public IFormFile? Document { get; set; }
}

public class UpdateVehicleComplianceFormDto : UpdateVehicleComplianceDto
{
    public IFormFile? Document { get; set; }
}

public class VehicleComplianceSummaryDto
{
    public int TotalDocuments { get; set; }
    public int Healthy { get; set; }
    public int DueSoon { get; set; }
    public int Overdue { get; set; }
}

public class VehicleComplianceListUiResponseDto
{
    public VehicleComplianceSummaryDto Summary { get; set; } = new();
    public PagedResultDto<VehicleComplianceDto> Documents { get; set; } = new();
}
