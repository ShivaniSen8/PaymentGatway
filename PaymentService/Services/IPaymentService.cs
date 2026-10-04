using PaymentService.DTOs;
using PaymentService.Gateways;

namespace PaymentService.Services;

public interface IPaymentService
{
    Task<PaymentResponse> CreatePaymentAsync(
        CreatePaymentRequest request);

    Task<PaymentResponse?> GetPaymentAsync(Guid paymentId);

    Task<PaymentResponse> VerifyPaymentAsync(
        Guid paymentId,
        PaymentGatewayVerificationRequest request);
}