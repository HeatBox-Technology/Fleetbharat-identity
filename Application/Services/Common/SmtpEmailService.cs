using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("Email send skipped because recipient address is empty.");
            return;
        }

        var host = _config["Email:SmtpHost"];
        var portValue = _config["Email:SmtpPort"];
        var username = _config["Email:Username"];
        var password = _config["Email:Password"];
        var fromEmail = _config["Email:FromEmail"];
        var fromName = _config["Email:FromName"];

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(portValue) ||
            !int.TryParse(portValue, out var port) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(fromEmail))
        {
            _logger.LogWarning(
                "Email send skipped because SMTP configuration is incomplete. HostConfigured={HostConfigured}, PortConfigured={PortConfigured}, UsernameConfigured={UsernameConfigured}, PasswordConfigured={PasswordConfigured}, FromEmailConfigured={FromEmailConfigured}",
                !string.IsNullOrWhiteSpace(host),
                int.TryParse(portValue, out _),
                !string.IsNullOrWhiteSpace(username),
                !string.IsNullOrWhiteSpace(password),
                !string.IsNullOrWhiteSpace(fromEmail));
            return;
        }

        using var message = new MailMessage();
        message.From = string.IsNullOrWhiteSpace(fromName)
            ? new MailAddress(fromEmail)
            : new MailAddress(fromEmail, fromName);
        message.To.Add(toEmail);
        message.Subject = subject;
        message.Body = htmlBody;
        message.IsBodyHtml = true;

        try
        {
            using var smtp = new SmtpClient(host, port);
            smtp.EnableSsl = true;
            smtp.Credentials = new NetworkCredential(username, password);

            await smtp.SendMailAsync(message);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP email send failed for recipient {Recipient}.", toEmail);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Unexpected email send failure for recipient {Recipient}.", toEmail);
        }
    }
}
