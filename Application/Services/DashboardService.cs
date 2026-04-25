using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public class DashboardService : IDashboardService
{
    private readonly ICurrentUserService _currentUser;

    public DashboardService(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public async Task<DashboardSummaryResponseDto> GetSummaryAsync(
        DashboardSummaryRequestDto request,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        ValidateRequest(request);

        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("Unauthorized");

        var requestedAccountIds = request.AccountIds
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        var accessibleRequestedAccountIds = requestedAccountIds
            .Select(x => new DashboardAccountScopeItem { AccountId = x })
            .AsQueryable()
            .ApplyAccountScope(_currentUser)
            .Select(x => x.AccountId)
            .ToList();

        if (accessibleRequestedAccountIds.Count != requestedAccountIds.Count)
            throw new UnauthorizedAccessException("Account access denied.");

        return await Task.FromResult(BuildDemoSummary());
    }

    private static void ValidateRequest(DashboardSummaryRequestDto request)
    {
        if (request == null)
            throw new BadHttpRequestException("Request body is required.");

        if (request.AccountIds == null || request.AccountIds.Count == 0)
            throw new BadHttpRequestException("AccountIds is required.");

        if (request.AccountIds.Any(x => x <= 0))
            throw new BadHttpRequestException("AccountIds must contain valid positive values.");

        if (request.FromDate == default)
            throw new BadHttpRequestException("FromDate is required.");

        if (request.ToDate == default)
            throw new BadHttpRequestException("ToDate is required.");

        if (request.ToDate.Date < request.FromDate.Date)
            throw new BadHttpRequestException("ToDate must be greater than or equal to FromDate.");
    }

    private static DashboardSummaryResponseDto BuildDemoSummary()
    {
        return new DashboardSummaryResponseDto
        {
            SummaryCards = new DashboardSummaryCardsDto
            {
                TotalVehicles = 1284,
                TotalDevices = 1450,
                TotalCustomers = 312,
                ActiveVehicles = 842,
                OfflineDevices = 42,
                AlertsToday = 28
            },
            DeviceInventory = new DashboardDeviceInventoryDto
            {
                InstalledDevices = 1284,
                AvailableDevices = 166
            },
            VehicleStatusDistribution = new DashboardVehicleStatusDistributionDto
            {
                Active = 842,
                Idle = 156,
                Maintenance = 244,
                Offline = 42
            },
            AlertsBreakdown = new DashboardAlertsBreakdownDto
            {
                Overspeeding = 9,
                GeofenceExit = 7,
                HarshBraking = 5,
                IdlingAlert = 4,
                SosAlert = 3
            },
            RecentAlerts = new DashboardRecentAlertsTableDto
            {
                TotalRecords = 5,
                Items = new List<DashboardRecentAlertDto>
                {
                    new()
                    {
                        VehicleNumber = "KA-01-HH-1234",
                        AlertType = "Overspeeding",
                        Time = "10:45 AM",
                        Status = "Critical"
                    },
                    new()
                    {
                        VehicleNumber = "MH-12-JK-5678",
                        AlertType = "Geofence Exit",
                        Time = "10:32 AM",
                        Status = "Warning"
                    },
                    new()
                    {
                        VehicleNumber = "DL-04-AB-9012",
                        AlertType = "Harsh Braking",
                        Time = "10:15 AM",
                        Status = "Warning"
                    },
                    new()
                    {
                        VehicleNumber = "KA-05-MN-3456",
                        AlertType = "SOS Alert",
                        Time = "09:58 AM",
                        Status = "Critical"
                    },
                    new()
                    {
                        VehicleNumber = "TN-07-XY-7890",
                        AlertType = "Idling Alert",
                        Time = "09:42 AM",
                        Status = "Info"
                    }
                }
            },
            ComplianceReminders = new DashboardComplianceRemindersTableDto
            {
                TotalRecords = 4,
                Items = new List<DashboardComplianceReminderDto>
                {
                    new()
                    {
                        VehicleNumber = "KA-01-HH-1234",
                        PucExpiry = new DateTime(2026, 4, 15),
                        InsuranceExpiry = new DateTime(2026, 5, 20),
                        Status = "Due Soon"
                    },
                    new()
                    {
                        VehicleNumber = "MH-12-JK-5678",
                        PucExpiry = new DateTime(2026, 4, 10),
                        InsuranceExpiry = new DateTime(2026, 6, 12),
                        Status = "Overdue"
                    },
                    new()
                    {
                        VehicleNumber = "DL-04-AB-9012",
                        PucExpiry = new DateTime(2026, 5, 5),
                        InsuranceExpiry = new DateTime(2026, 4, 25),
                        Status = "Due Soon"
                    },
                    new()
                    {
                        VehicleNumber = "KA-05-MN-3456",
                        PucExpiry = new DateTime(2026, 6, 20),
                        InsuranceExpiry = new DateTime(2026, 7, 15),
                        Status = "Healthy"
                    }
                }
            }
        };
    }

    private sealed class DashboardAccountScopeItem : IAccountEntity
    {
        public int AccountId { get; set; }
    }
}
