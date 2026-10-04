using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PaymentService.DTOs;
using PaymentService.Exceptions;

namespace PaymentService.Gateways;

public sealed class RazorpayPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public RazorpayPaymentGateway(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public string KeyId => _configuration["Razorpay:KeyId"] ?? string.Empty;

    public async Task<PaymentGatewayResponse> CreatePaymentAsync(
        PaymentGatewayRequest request)
    {
        var (keyId, keySecret) = GetCredentials();
        var amountInMinorUnits = request.Amount * 100m;

        if (request.Amount <= 0 ||
            amountInMinorUnits != decimal.Truncate(amountInMinorUnits))
        {
            throw new ArgumentException(
                "Payment amount must be positive and use at most two decimal places.");
        }

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            "v1/orders")
        {
            Content = JsonContent.Create(new
            {
                amount = decimal.ToInt64(amountInMinorUnits),
                currency = request.Currency,
                receipt = request.PaymentId.ToString("N"),
                notes = new
                {
                    payment_id = request.PaymentId,
                    order_id = request.OrderId
                }
            })
        };

        message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{keyId}:{keySecret}")));

        try
        {
            using var response = await _httpClient.SendAsync(message);

            if (!response.IsSuccessStatusCode)
            {
                throw new PaymentProviderException(
                    $"Razorpay order creation failed with status {(int)response.StatusCode}.");
            }

            var razorpayOrder =
                await response.Content.ReadFromJsonAsync<RazorpayOrderResponse>();

            if (string.IsNullOrWhiteSpace(razorpayOrder?.Id))
            {
                throw new PaymentProviderException(
                    "Razorpay returned an invalid order response.");
            }

            return new PaymentGatewayResponse
            {
                Success = true,
                ProviderOrderId = razorpayOrder.Id,
                Message = "Razorpay order created."
            };
        }
        catch (HttpRequestException ex)
        {
            throw new PaymentProviderException(
                "Unable to reach Razorpay.",
                ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new PaymentProviderException(
                "The Razorpay request timed out.",
                ex);
        }
        catch (JsonException ex)
        {
            throw new PaymentProviderException(
                "Razorpay returned an invalid response.",
                ex);
        }
    }

    public Task<PaymentGatewayVerificationResponse> VerifyPaymentAsync(
        PaymentGatewayVerificationRequest request)
    {
        var (_, keySecret) = GetCredentials();

        if (string.IsNullOrWhiteSpace(request.ProviderOrderId) ||
            string.IsNullOrWhiteSpace(request.ProviderPaymentId) ||
            string.IsNullOrWhiteSpace(request.Signature))
        {
            return Task.FromResult(new PaymentGatewayVerificationResponse
            {
                Success = false,
                Message = "Payment verification details are incomplete."
            });
        }

        byte[] suppliedSignature;
        try
        {
            suppliedSignature = Convert.FromHexString(request.Signature);
        }
        catch (FormatException)
        {
            return Task.FromResult(new PaymentGatewayVerificationResponse
            {
                Success = false,
                Message = "Payment signature is invalid."
            });
        }

        var signedPayload =
            $"{request.ProviderOrderId}|{request.ProviderPaymentId}";
        var expectedSignature = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(keySecret),
            Encoding.UTF8.GetBytes(signedPayload));
        var isValid = suppliedSignature.Length == expectedSignature.Length &&
            CryptographicOperations.FixedTimeEquals(
                suppliedSignature,
                expectedSignature);

        return Task.FromResult(new PaymentGatewayVerificationResponse
        {
            Success = isValid,
            Message = isValid
                ? "Razorpay payment signature verified."
                : "Payment signature is invalid."
        });
    }

    private (string KeyId, string KeySecret) GetCredentials()
    {
        var keyId = _configuration["Razorpay:KeyId"];
        var keySecret = _configuration["Razorpay:KeySecret"];

        if (string.IsNullOrWhiteSpace(keyId) ||
            string.IsNullOrWhiteSpace(keySecret))
        {
            throw new PaymentProviderException(
                "Razorpay test credentials are not configured. Set Razorpay__KeyId and Razorpay__KeySecret.");
        }

        return (keyId, keySecret);
    }

    private sealed class RazorpayOrderResponse
    {
        public string? Id { get; set; }
    }
}
