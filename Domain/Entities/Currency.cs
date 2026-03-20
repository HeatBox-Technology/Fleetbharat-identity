using System;

public class Currency
{
    public int CurrencyId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Symbol { get; set; } = null!;

    public string Country { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
