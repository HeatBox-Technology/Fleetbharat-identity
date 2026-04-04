using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(HttpClient http, IConfiguration config, ILogger<WhatsAppService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task SendTemplateAsync(WhatsAppTemplateMessage message)
    {
        var accessToken =
            _config["WhatsApp:AccessToken"] ??
            _config["WHATSAPP_ACCESS_TOKEN"] ??
            _config["WHATSAPP_TOKEN"];

        var phoneNumberId =
            _config["WhatsApp:PhoneNumberId"] ??
            _config["WHATSAPP_PHONE_NUMBER_ID"];

        var apiVersion = _config["WhatsApp:ApiVersion"] ?? "v22.0";

        if (string.IsNullOrWhiteSpace(accessToken))
            throw new InvalidOperationException("Missing WhatsApp access token configuration.");

        if (string.IsNullOrWhiteSpace(phoneNumberId))
            throw new InvalidOperationException("Missing WhatsApp phone number id configuration.");

        if (string.IsNullOrWhiteSpace(message.To))
            throw new InvalidOperationException("WhatsApp recipient number is required.");

        if (string.IsNullOrWhiteSpace(message.TemplateName))
            throw new InvalidOperationException("WhatsApp template name is required.");

        var components = new List<object>();

        if (message.BodyVariables is { Count: > 0 })
        {
            components.Add(new
            {
                type = "body",
                parameters = BuildTextParameters(message.BodyVariables)
            });
        }

        if (!string.IsNullOrWhiteSpace(message.ButtonVariable))
        {
            components.Add(new
            {
                type = "button",
                sub_type = "url",
                index = "0",
                parameters = new[]
                {
                    new { type = "text", text = message.ButtonVariable }
                }
            });
        }

        var payload = new
        {
            messaging_product = "whatsapp",
            to = NormalizePhone(message.To),
            type = "template",
            template = new
            {
                name = message.TemplateName,
                language = new { code = message.LanguageCode },
                components = components.Count == 0 ? null : components
            }
        };

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://graph.facebook.com/{apiVersion}/{phoneNumberId}/messages");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }),
            Encoding.UTF8,
            "application/json");

        var response = await _http.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("WhatsApp API error {StatusCode}: {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"WhatsApp API error ({(int)response.StatusCode}).");
        }
    }

    private static IEnumerable<object> BuildTextParameters(IReadOnlyList<string> values)
    {
        for (var i = 0; i < values.Count; i++)
        {
            yield return new { type = "text", text = values[i] ?? string.Empty };
        }
    }

    private static string NormalizePhone(string phone)
    {
        var digits = new StringBuilder();
        foreach (var ch in phone)
        {
            if (char.IsDigit(ch))
                digits.Append(ch);
        }

        return digits.ToString();
    }
}
