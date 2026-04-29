using System;
using System.Text.Json;

public class RealtimeEventMessage
{
    public string Topic { get; set; } = string.Empty;
    public int? OrgId { get; set; }
    public string? Key { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public JsonElement Payload { get; set; }
    public string Source { get; set; } = "kafka";
}
