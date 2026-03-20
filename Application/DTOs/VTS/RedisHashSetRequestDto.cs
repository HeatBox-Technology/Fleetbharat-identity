namespace Application.DTOs;

public class RedisHashSetRequestDto
{
    public string HashKey { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public object? Value { get; set; }
}
