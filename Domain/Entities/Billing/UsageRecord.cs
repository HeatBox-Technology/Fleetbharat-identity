using System;

public class UsageRecord : IAccountEntity
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int SubscriptionId { get; set; }
    public string UsageType { get; set; } = "";
    public decimal UnitsConsumed { get; set; }
    public DateTime UsageDate { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }

    public AccountSubscription? Subscription { get; set; }
}
