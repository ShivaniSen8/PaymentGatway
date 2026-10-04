namespace PaymentService.Gateways;

public class PaymentGatewayResponse
{
    public bool Success { get; set; }

    public string? ProviderOrderId { get; set; }

    public string? Message { get; set; }
}