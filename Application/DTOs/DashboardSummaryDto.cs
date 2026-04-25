using System;
using System.Collections.Generic;

public class DashboardSummaryRequestDto
{
    public List<int> AccountIds { get; set; } = new();
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}

public class DashboardSummaryResponseDto
{
    public DashboardSummaryCardsDto SummaryCards { get; set; } = new();
    public DashboardDeviceInventoryDto DeviceInventory { get; set; } = new();
    public DashboardVehicleStatusDistributionDto VehicleStatusDistribution { get; set; } = new();
    public DashboardAlertsBreakdownDto AlertsBreakdown { get; set; } = new();
    public DashboardRecentAlertsTableDto RecentAlerts { get; set; } = new();
    public DashboardComplianceRemindersTableDto ComplianceReminders { get; set; } = new();
}

public class DashboardSummaryCardsDto
{
    public int TotalVehicles { get; set; }
    public int TotalDevices { get; set; }
    public int TotalCustomers { get; set; }
    public int ActiveVehicles { get; set; }
    public int OfflineDevices { get; set; }
    public int AlertsToday { get; set; }
}

public class DashboardDeviceInventoryDto
{
    public int InstalledDevices { get; set; }
    public int AvailableDevices { get; set; }
}

public class DashboardVehicleStatusDistributionDto
{
    public int Active { get; set; }
    public int Idle { get; set; }
    public int Maintenance { get; set; }
    public int Offline { get; set; }
}

public class DashboardAlertsBreakdownDto
{
    public int Overspeeding { get; set; }
    public int GeofenceExit { get; set; }
    public int HarshBraking { get; set; }
    public int IdlingAlert { get; set; }
    public int SosAlert { get; set; }
}

public class DashboardRecentAlertsTableDto
{
    public int TotalRecords { get; set; }
    public List<DashboardRecentAlertDto> Items { get; set; } = new();
}

public class DashboardRecentAlertDto
{
    public string VehicleNumber { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class DashboardComplianceRemindersTableDto
{
    public int TotalRecords { get; set; }
    public List<DashboardComplianceReminderDto> Items { get; set; } = new();
}

public class DashboardComplianceReminderDto
{
    public string VehicleNumber { get; set; } = string.Empty;
    public DateTime PucExpiry { get; set; }
    public DateTime InsuranceExpiry { get; set; }
    public string Status { get; set; } = string.Empty;
}
