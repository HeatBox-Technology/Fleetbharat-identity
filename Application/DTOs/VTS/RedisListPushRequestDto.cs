namespace Application.DTOs;

public class RedisListPushRequestDto
{
    public string ListKey { get; set; } = string.Empty;
    public object? Value { get; set; }
}
