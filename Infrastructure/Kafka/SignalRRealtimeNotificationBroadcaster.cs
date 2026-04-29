using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;

public class SignalRRealtimeNotificationBroadcaster : IRealtimeNotificationBroadcaster
{
    private readonly IHubContext<TrackingHub> _hub;

    public SignalRRealtimeNotificationBroadcaster(IHubContext<TrackingHub> hub)
    {
        _hub = hub;
    }

    public async Task BroadcastAsync(RealtimeEventMessage message, CancellationToken ct = default)
    {
        var normalizedTopic = RealtimeGroupNames.NormalizeTopic(message.Topic);

        await _hub.Clients.All.SendAsync("realtime_event", message, ct);
        await _hub.Clients.Group(RealtimeGroupNames.Topic(normalizedTopic))
            .SendAsync("realtime_event", message, ct);

        if (message.OrgId.HasValue)
        {
            await _hub.Clients.Group(RealtimeGroupNames.Org(message.OrgId.Value))
                .SendAsync("realtime_event", message, ct);
            await _hub.Clients.Group(RealtimeGroupNames.OrgTopic(message.OrgId.Value, normalizedTopic))
                .SendAsync("realtime_event", message, ct);
        }

        if (normalizedTopic == "alerts")
        {
            var alert = JsonSerializer.Deserialize<VehicleAlertMessage>(message.Payload.GetRawText());
            if (alert != null)
            {
                await _hub.Clients.All.SendAsync("alert_update", alert, ct);
                await _hub.Clients.Group(RealtimeGroupNames.Topic(normalizedTopic))
                    .SendAsync("alert_update", alert, ct);
                await _hub.Clients.Group(RealtimeGroupNames.Org(alert.OrgId))
                    .SendAsync("alert_update", alert, ct);
                await _hub.Clients.Group(RealtimeGroupNames.OrgTopic(alert.OrgId, normalizedTopic))
                    .SendAsync("alert_update", alert, ct);

                if (!string.IsNullOrWhiteSpace(alert.VehicleId))
                {
                    await _hub.Clients.Group($"vehicle:{alert.VehicleId.Trim()}")
                        .SendAsync("alert_update", alert, ct);
                }
            }

            return;
        }

        if (normalizedTopic == "gps")
        {
            var deviceId = TryGetString(message.Payload, "deviceId")
                ?? TryGetString(message.Payload, "deviceNo")
                ?? TryGetString(message.Payload, "imei");

            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                await _hub.Clients.Group($"device:{deviceId.Trim()}")
                    .SendAsync("gps_update", message.Payload, ct);
            }
        }
    }

    private static string? TryGetString(JsonElement payload, string propertyName)
    {
        if (payload.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var property in payload.EnumerateObject())
        {
            if (!string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                continue;

            return property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number => property.Value.GetRawText(),
                _ => property.Value.GetRawText()
            };
        }

        return null;
    }
}
