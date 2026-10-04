namespace PaymentService.DTOs;

public class CreatePaymentRequest
{
    public Guid OrderId { get; set; }

    public string? IdempotencyKey { get; set; }
}