using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;

    public SmtpEmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var host = _config["Email:SmtpHost"]!;
        var port = int.Parse(_config["Email:SmtpPort"]!);
        var username = _config["Email:Username"]!;
        var password = _config["Email:Password"]!;
        var fromEmail = _config["Email:FromEmail"]!;
        var fromName = _config["Email:FromName"]!;

        using var message = new MailMessage();
        message.From = new MailAddress(fromEmail, fromName);
        message.To.Add(toEmail);
        message.Subject = subject;
        message.Body = htmlBody;
        message.IsBodyHtml = true;

        using var smtp = new SmtpClient(host, port);
        smtp.EnableSsl = true;
        smtp.Credentials = new NetworkCredential(username, password);

        await smtp.SendMailAsync(message);
    }
}
