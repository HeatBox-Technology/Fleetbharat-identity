using System.Collections.Generic;
using System.Threading.Tasks;

public interface IWhatsAppService
{
    Task SendTemplateAsync(WhatsAppTemplateMessage message);
}

public class WhatsAppTemplateMessage
{
    public string To { get; set; } = "";
    public string TemplateName { get; set; } = "";
    public string LanguageCode { get; set; } = "en_US";
    public IReadOnlyList<string> BodyVariables { get; set; } = System.Array.Empty<string>();
    public string? ButtonVariable { get; set; }
}
