using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("error_logs")]
public class ErrorLog
{
    [Key]
    public int id { get; set; }
    public string message { get; set; } = string.Empty;
    public string stack_trace { get; set; } = string.Empty;
    public string inner_exception { get; set; } = string.Empty;
    public string path { get; set; } = string.Empty;
    public string method { get; set; } = string.Empty;
    public DateTime created_at { get; set; }
}
