using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.DTOs;
using PaymentService.Exceptions;
using PaymentService.Gateways;
using PaymentService.Models;

namespace PaymentService.Services;

public class PaymentApplicationService : IPaymentService
{
    private readonly PaymentDbContext _context;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PaymentApplicationService(
        PaymentDbContext context,
        IPaymentGateway paymentGateway,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _paymentGateway = paymentGateway;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PaymentResponse> CreatePaymentAsync(
        CreatePaymentRequest request)
    {
        if (request.OrderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID is required.");
        }

        var order = await GetOwnedOrderAsync(request.OrderId);

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(
                    payment => payment.IdempotencyKey == request.IdempotencyKey);

            if (existingPayment != null)
            {
                if (existingPayment.OrderId != order.Id ||
                    existingPayment.UserId != order.UserId.ToString())
                {
                    throw new InvalidOperationException(
                        "The idempotency key has already been used.");
                }

                if (existingPayment.Status == PaymentStatus.Succeeded)
                {
                    await ConfirmOrderAsync(existingPayment.OrderId);
                }

                return MapResponse(existingPayment);
            }
        }

        if (!order.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Only pending orders can be paid.");
        }

        if (order.TotalAmount <= 0)
        {
            throw new ArgumentException(
                "The order total must be greater than zero.");
        }

        var amountInMinorUnits = order.TotalAmount * 100m;
        if (amountInMinorUnits != decimal.Truncate(amountInMinorUnits))
        {
            throw new ArgumentException(
                "The order total must use at most two decimal places.");
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            UserId = order.UserId.ToString(),
            Amount = order.TotalAmount,
            Currency = "INR",
            Status = PaymentStatus.Created,
            PaymentProvider = "Razorpay",
            IdempotencyKey = request.IdempotencyKey,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        PaymentGatewayResponse gatewayResponse;
        try
        {
            gatewayResponse = await _paymentGateway.CreatePaymentAsync(
                new PaymentGatewayRequest
                {
                    PaymentId = payment.Id,
                    OrderId = payment.OrderId,
                    Amount = payment.Amount,
                    Currency = payment.Currency
                });
        }
        catch (PaymentProviderException)
        {
            payment.Status = PaymentStatus.Failed;
            payment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            throw;
        }

        if (!gatewayResponse.Success ||
            string.IsNullOrWhiteSpace(gatewayResponse.ProviderOrderId))
        {
            payment.Status = PaymentStatus.Failed;
            payment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            throw new PaymentProviderException(
                "Razorpay did not create a payment order.");
        }

        payment.ProviderOrderId = gatewayResponse.ProviderOrderId;
        payment.Status = PaymentStatus.Pending;
        payment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapResponse(payment);
    }

    public async Task<PaymentResponse?> GetPaymentAsync(Guid paymentId)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(item => item.Id == paymentId);

        if (payment == null)
        {
            return null;
        }

        var order = await GetOwnedOrderAsync(payment.OrderId);
        EnsurePaymentOrderOwner(payment, order);

        if (payment.Status == PaymentStatus.Succeeded)
        {
            await ConfirmOrderAsync(payment.OrderId);
        }

        return MapResponse(payment);
    }

    public async Task<PaymentResponse> VerifyPaymentAsync(
        Guid paymentId,
        PaymentGatewayVerificationRequest request)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(item => item.Id == paymentId);

        if (payment == null)
        {
            throw new KeyNotFoundException("Payment not found.");
        }

        var order = await GetOwnedOrderAsync(payment.OrderId);
        EnsurePaymentOrderOwner(payment, order);

        if (payment.Status == PaymentStatus.Succeeded &&
            payment.ProviderOrderId == request.ProviderOrderId &&
            payment.ProviderPaymentId == request.ProviderPaymentId)
        {
            await ConfirmOrderAsync(payment.OrderId);
            return MapResponse(payment);
        }

        if (payment.Status != PaymentStatus.Pending ||
            string.IsNullOrWhiteSpace(payment.ProviderOrderId) ||
            !string.Equals(
                payment.ProviderOrderId,
                request.ProviderOrderId,
                StringComparison.Ordinal))
        {
            throw new PaymentVerificationException(
                "The payment order does not match.");
        }

        request.PaymentId = payment.Id;
        var verification = await _paymentGateway.VerifyPaymentAsync(request);

        if (!verification.Success)
        {
            throw new PaymentVerificationException(
                "Razorpay payment signature verification failed.");
        }

        payment.ProviderPaymentId = request.ProviderPaymentId;
        payment.Status = PaymentStatus.Succeeded;
        payment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await ConfirmOrderAsync(payment.OrderId);
        return MapResponse(payment);
    }

    private async Task<OrderDetails> GetOwnedOrderAsync(Guid orderId)
    {
        var authorization = _httpContextAccessor.HttpContext?
            .Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authorization))
        {
            throw new UnauthorizedAccessException(
                "An authenticated session is required.");
        }

        using var orderRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/orders/{orderId}");
        orderRequest.Headers.TryAddWithoutValidation(
            "Authorization",
            authorization);

        HttpResponseMessage response;
        try
        {
            response = await _httpClientFactory
                .CreateClient("OrderService")
                .SendAsync(orderRequest);
        }
        catch (HttpRequestException ex)
        {
            throw new OrderServiceException(
                "Unable to reach the order service.",
                ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OrderServiceException(
                "The order service request timed out.",
                ex);
        }

        using (response)
        {
            if (response.StatusCode is HttpStatusCode.Unauthorized or
                HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException(
                    "The order could not be accessed with this session.");
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new KeyNotFoundException("Order not found.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new OrderServiceException(
                    $"Order service returned {(int)response.StatusCode}.");
            }

            try
            {
                return await response.Content.ReadFromJsonAsync<OrderDetails>()
                    ?? throw new OrderServiceException(
                        "Order service returned an empty order.");
            }
            catch (System.Text.Json.JsonException ex)
            {
                throw new OrderServiceException(
                    "Order service returned an invalid order response.",
                    ex);
            }
        }
    }

    private static void EnsurePaymentOrderOwner(
        Payment payment,
        OrderDetails order)
    {
        if (payment.OrderId != order.Id ||
            payment.UserId != order.UserId.ToString())
        {
            throw new UnauthorizedAccessException(
                "The payment does not belong to this order.");
        }
    }

    private async Task ConfirmOrderAsync(Guid orderId)
    {
        var authorization = _httpContextAccessor.HttpContext?
            .Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authorization))
        {
            throw new UnauthorizedAccessException(
                "An authenticated session is required.");
        }

        using var orderRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/orders/{orderId}/confirm-payment");
        orderRequest.Headers.TryAddWithoutValidation(
            "Authorization",
            authorization);

        HttpResponseMessage response;
        try
        {
            response = await _httpClientFactory
                .CreateClient("OrderService")
                .SendAsync(orderRequest);
        }
        catch (HttpRequestException ex)
        {
            throw new OrderServiceException(
                "Unable to reach the order service to confirm payment.",
                ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OrderServiceException(
                "The order confirmation request timed out.",
                ex);
        }

        using (response)
        {
            if (response.StatusCode is HttpStatusCode.Unauthorized or
                HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException(
                    "The order could not be confirmed with this session.");
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new KeyNotFoundException("Order not found.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new OrderServiceException(
                    $"Order confirmation failed with status {(int)response.StatusCode}.");
            }
        }
    }

    private PaymentResponse MapResponse(Payment payment)
    {
        return new PaymentResponse
        {
            PaymentId = payment.Id,
            OrderId = payment.OrderId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = payment.Status,
            PaymentProvider = payment.PaymentProvider,
            ProviderOrderId = payment.ProviderOrderId,
            RazorpayKeyId = _paymentGateway.KeyId
        };
    }

    private sealed class OrderDetails
    {
        public Guid Id { get; set; }

        public int UserId { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}
