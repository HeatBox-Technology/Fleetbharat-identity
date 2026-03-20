using System;

public class AuditLog
{
    public long Id { get; set; }
    public int? AccountId { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string ServiceName { get; set; } = "";
    public string? Module { get; set; }
    public string? Action { get; set; }
    public string Endpoint { get; set; } = "";
    public string EventType { get; set; } = "";
    public int Status { get; set; }
    public long ExecutionTimeMs { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
