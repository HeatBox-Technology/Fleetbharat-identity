using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("error_logs")]
public class ErrorLog
{
    [Key]
    public int id { get; set; }
    public string message { get; set; }
    public string stack_trace { get; set; }
    public string inner_exception { get; set; }
    public string path { get; set; }
    public string method { get; set; }
    public DateTime created_at { get; set; }
}