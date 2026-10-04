namespace PaymentService.Gateways;

public class PaymentGatewayVerificationRequest
{
    public Guid PaymentId { get; set; }

    public string ProviderOrderId { get; set; } = string.Empty;

    public string ProviderPaymentId { get; set; } = string.Empty;

    public string Signature { get; set; } = string.Empty;
}