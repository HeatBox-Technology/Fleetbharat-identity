namespace Application.DTOs;

public class RedisSetRequestDto
{
    public string Key { get; set; } = string.Empty;
    public object? Value { get; set; }
    public int? TtlSeconds { get; set; } // optional
}
