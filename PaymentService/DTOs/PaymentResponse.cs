using PaymentService.Models;

namespace PaymentService.DTOs;

public class PaymentResponse
{
    public Guid PaymentId { get; set; }

    public Guid OrderId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "INR";

    public PaymentStatus Status { get; set; }

    public string? PaymentProvider { get; set; }

    public string? ProviderOrderId { get; set; }

    public string RazorpayKeyId { get; set; } = string.Empty;
}