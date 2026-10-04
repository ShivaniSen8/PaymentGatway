namespace PaymentService.Models;

public class Payment
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "INR";

    public PaymentStatus Status { get; set; }

    public string? PaymentProvider { get; set; }

    public string? ProviderOrderId { get; set; }

    public string? ProviderPaymentId { get; set; }

    public string? IdempotencyKey { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}