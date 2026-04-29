using System;

public class KafkaRealtimeOptions
{
    public const string SectionName = "KafkaRealtime";

    public bool Enabled { get; set; }
    public string BootstrapServers { get; set; } = string.Empty;
    public string GroupId { get; set; } = "fleetbharat-realtime-consumer";
    public string[] Topics { get; set; } = Array.Empty<string>();
    public string AutoOffsetReset { get; set; } = "Latest";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? SecurityProtocol { get; set; }
    public string? SaslMechanism { get; set; }
}
