using System;

public class BillingInvoice : IAccountEntity
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public int SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Status { get; set; } = "Pending";
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryDate { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }

    public AccountSubscription? Subscription { get; set; }
}
