using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

[ApiController]
[Route("api/kafka-test")]
public class KafkaTestController : ControllerBase
{
    private readonly IKafkaAlertPublisher _publisher;

    public KafkaTestController(IKafkaAlertPublisher publisher)
    {
        _publisher = publisher;
    }

    [HttpPost("alerts")]
    public async Task<IActionResult> PublishAlert([FromBody] PublishAlertRequest? request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var alert = new VehicleAlertMessage
        {
            OrgId = request?.OrgId ?? 1,
            VehicleId = string.IsNullOrWhiteSpace(request?.VehicleId) ? "275" : request!.VehicleId.Trim(),
            VehicleNo = string.IsNullOrWhiteSpace(request?.VehicleNo) ? "TEST-ISTARTEK" : request!.VehicleNo.Trim(),
            DeviceNo = string.IsNullOrWhiteSpace(request?.DeviceNo) ? "861245058643742" : request!.DeviceNo.Trim(),
            Imei = string.IsNullOrWhiteSpace(request?.Imei) ? "861245058643742" : request!.Imei.Trim(),
            Type = string.IsNullOrWhiteSpace(request?.Type) ? "Ignition" : request!.Type.Trim(),
            Status = string.IsNullOrWhiteSpace(request?.Status) ? "OFF" : request!.Status.Trim(),
            Latitude = request?.Latitude ?? 5.3862,
            Longitude = request?.Longitude ?? 100.3046,
            Address = request?.Address ?? string.Empty,
            GpsDate = request?.GpsDate ?? now.AddMinutes(-1),
            ReceivedTime = request?.ReceivedTime ?? now,
            Severity = string.IsNullOrWhiteSpace(request?.Severity) ? "Normal" : request!.Severity.Trim()
        };

        await _publisher.PublishAsync(alert, ct);

        return Ok(new
        {
            ok = true,
            topic = "alerts",
            key = alert.VehicleId,
            data = alert
        });
    }
}

public class PublishAlertRequest
{
    public int? OrgId { get; set; }
    public string? VehicleId { get; set; }
    public string? VehicleNo { get; set; }
    public string? DeviceNo { get; set; }
    public string? Imei { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Address { get; set; }
    public DateTime? GpsDate { get; set; }
    public DateTime? ReceivedTime { get; set; }
    public string? Severity { get; set; }
}
