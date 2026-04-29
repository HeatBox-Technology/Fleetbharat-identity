using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class KafkaRealtimeConsumerWorker : BackgroundService
{
    private readonly ILogger<KafkaRealtimeConsumerWorker> _logger;
    private readonly KafkaRealtimeOptions _options;
    private readonly IRealtimeNotificationBroadcaster _broadcaster;

    public KafkaRealtimeConsumerWorker(
        ILogger<KafkaRealtimeConsumerWorker> logger,
        IOptions<KafkaRealtimeOptions> options,
        IRealtimeNotificationBroadcaster broadcaster)
    {
        _logger = logger;
        _options = options.Value ?? new KafkaRealtimeOptions();
        _broadcaster = broadcaster;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("KafkaRealtimeConsumerWorker is disabled");
            return;
        }

        var topics = _options.Topics
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (string.IsNullOrWhiteSpace(_options.BootstrapServers) || topics.Length == 0)
        {
            _logger.LogWarning("KafkaRealtimeConsumerWorker is enabled but Kafka configuration is incomplete");
            return;
        }

        // Yield once so the generic host can finish startup and bind HTTP endpoints
        // before this worker enters the blocking Kafka consume loop.
        await Task.Yield();

        var consumerConfig = BuildConsumerConfig();

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetErrorHandler((_, error) =>
            {
                _logger.LogWarning("Kafka consumer error. Code: {Code}, Reason: {Reason}", error.Code, error.Reason);
            })
            .Build();

        consumer.Subscribe(topics);

        _logger.LogInformation(
            "KafkaRealtimeConsumerWorker subscribed to topics: {Topics}",
            topics);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                if (result?.Message == null || string.IsNullOrWhiteSpace(result.Message.Value))
                    continue;

                var realtimeMessage = BuildRealtimeMessage(result);
                await _broadcaster.BroadcastAsync(realtimeMessage, stoppingToken);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume exception for topic {Topic}", ex.ConsumerRecord?.Topic);
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Kafka message payload is not valid JSON");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in KafkaRealtimeConsumerWorker");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        consumer.Close();
    }

    private ConsumerConfig BuildConsumerConfig()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = string.IsNullOrWhiteSpace(_options.GroupId)
                ? "fleetbharat-realtime-consumer"
                : _options.GroupId,
            EnableAutoCommit = true,
            AutoOffsetReset = ParseAutoOffsetReset(_options.AutoOffsetReset),
            AllowAutoCreateTopics = false
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

    private static AutoOffsetReset ParseAutoOffsetReset(string? value)
    {
        return Enum.TryParse<AutoOffsetReset>(value, true, out var parsed)
            ? parsed
            : AutoOffsetReset.Latest;
    }

    private static RealtimeEventMessage BuildRealtimeMessage(ConsumeResult<string, string> result)
    {
        using var doc = JsonDocument.Parse(result.Message.Value);
        var root = doc.RootElement.Clone();
        var topic = TryGetString(root, "topic") ?? result.Topic;

        if (!string.Equals(topic, "alerts", StringComparison.OrdinalIgnoreCase))
        {
            throw new JsonException($"Unsupported topic '{topic}'. This worker currently handles only the alerts topic.");
        }

        return new RealtimeEventMessage
        {
            Topic = topic,
            OrgId = TryGetInt(root, "orgId") ?? TryGetInt(root, "accountId"),
            Key = string.IsNullOrWhiteSpace(result.Message.Key)
                ? TryGetString(root, "vehicleId")
                : result.Message.Key,
            ReceivedAt = DateTimeOffset.UtcNow,
            Payload = root,
            Source = "kafka"
        };
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

    private static int? TryGetInt(JsonElement payload, string propertyName)
    {
        var text = TryGetString(payload, propertyName);
        return int.TryParse(text, out var value) ? value : null;
    }
}
