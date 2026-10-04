namespace PaymentService.Gateways;

public interface IPaymentGateway
{
    string KeyId { get; }

    Task<PaymentGatewayResponse> CreatePaymentAsync(
        PaymentGatewayRequest request);

    Task<PaymentGatewayVerificationResponse>
        VerifyPaymentAsync(
            PaymentGatewayVerificationRequest request);
}