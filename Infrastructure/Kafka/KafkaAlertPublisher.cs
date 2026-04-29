using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class KafkaAlertPublisher : IKafkaAlertPublisher
{
    private readonly KafkaRealtimeOptions _options;
    private readonly ILogger<KafkaAlertPublisher> _logger;

    public KafkaAlertPublisher(
        IOptions<KafkaRealtimeOptions> options,
        ILogger<KafkaAlertPublisher> logger)
    {
        _options = options.Value ?? new KafkaRealtimeOptions();
        _logger = logger;
    }

    public async Task PublishAsync(VehicleAlertMessage message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BootstrapServers))
            throw new InvalidOperationException("Kafka bootstrap servers are not configured.");

        var topic = _options.Topics
            .FirstOrDefault(x => string.Equals(x, "alerts", StringComparison.OrdinalIgnoreCase))
            ?? "alerts";

        var producerConfig = BuildProducerConfig();

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        var payload = JsonSerializer.Serialize(new
        {
            orgId = message.OrgId,
            vehicleId = message.VehicleId,
            vehicleNo = message.VehicleNo,
            deviceNo = message.DeviceNo,
            imei = message.Imei,
            type = message.Type,
            status = message.Status,
            latitude = message.Latitude,
            longitude = message.Longitude,
            address = message.Address,
            gpsDate = message.GpsDate,
            receivedTime = message.ReceivedTime,
            severity = message.Severity
        });

        var result = await producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = message.VehicleId,
            Value = payload
        }, ct);

        _logger.LogInformation(
            "Published Kafka alert for vehicle {VehicleId} to topic {Topic}, partition {Partition}, offset {Offset}",
            message.VehicleId,
            topic,
            result.Partition.Value,
            result.Offset.Value);
    }

    private ProducerConfig BuildProducerConfig()
    {
        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            AllowAutoCreateTopics = true
        };

        if (!string.IsNullOrWhiteSpace(_options.SecurityProtocol) &&
            Enum.TryParse<SecurityProtocol>(_options.SecurityProtocol, true, out var securityProtocol))
        {
            config.SecurityProtocol = securityProtocol;
        }

        if (!string.IsNullOrWhiteSpace(_options.SaslMechanism) &&
            Enum.TryParse<SaslMechanism>(_options.SaslMechanism, true, out var saslMechanism))
        {
            config.SaslMechanism = saslMechanism;
        }

        if (!string.IsNullOrWhiteSpace(_options.Username))
            config.SaslUsername = _options.Username;

        if (!string.IsNullOrWhiteSpace(_options.Password))
            config.SaslPassword = _options.Password;

        return config;
    }
}
