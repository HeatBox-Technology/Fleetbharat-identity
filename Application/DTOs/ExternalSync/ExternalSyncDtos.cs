using System;
using System.Collections.Generic;

public class ExternalSyncQueueCreateRequest
{
    public string ModuleName { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string PayloadJson { get; set; } = "";
}

public class ExternalSyncModuleStatsDto
{
    public string Module { get; set; } = "";
    public int Pending { get; set; }
    public int Processing { get; set; }
    public int Success { get; set; }
    public int Failed { get; set; }
    public int DLQ { get; set; }
}

public class ExternalSyncDashboardDto
{
    public List<ExternalSyncModuleStatsDto> Items { get; set; } = new();
}

public class ExternalSyncQueueItemDto
{
    public long Id { get; set; }
    public string ModuleName { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Status { get; set; } = "";
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ExternalSyncDlqItemDto
{
    public long Id { get; set; }
    public string ModuleName { get; set; } = "";
    public string EntityId { get; set; } = "";
    public int RetryCount { get; set; }
    public string ErrorMessage { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime MovedToDLQAt { get; set; }
}
