namespace PaymentService.Gateways;

public class PaymentGatewayRequest
{
    public Guid PaymentId { get; set; }

    public Guid OrderId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "INR";
}