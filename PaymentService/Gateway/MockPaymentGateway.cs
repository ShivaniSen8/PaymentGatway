namespace PaymentService.Gateways;

public class MockPaymentGateway : IPaymentGateway
{
    public string KeyId => string.Empty;

    public Task<PaymentGatewayResponse> CreatePaymentAsync(
        PaymentGatewayRequest request)
    {
        var response = new PaymentGatewayResponse
        {
            Success = true,
            ProviderOrderId = $"MOCK_ORDER_{Guid.NewGuid()}",
            Message = "Mock payment order created successfully."
        };

        return Task.FromResult(response);
    }

    public Task<PaymentGatewayVerificationResponse>
        VerifyPaymentAsync(
            PaymentGatewayVerificationRequest request)
    {
        var response = new PaymentGatewayVerificationResponse
        {
            Success = true,
            Message = "Mock payment verified successfully."
        };

        return Task.FromResult(response);
    }
}