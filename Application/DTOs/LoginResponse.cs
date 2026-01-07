using System;

public record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);