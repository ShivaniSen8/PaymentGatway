# Payment Service

ASP.NET Core 8 payment API using Razorpay Orders and server-side checkout signature verification.

## Configuration

Set configuration through environment variables; do not commit API keys or database credentials.

```powershell
$env:ConnectionStrings__PaymentDb = "<sql-server-connection-string>"
$env:OrderService__BaseUrl = "https://localhost:7112/"
$env:Razorpay__KeyId = "rzp_test_<key-id>"
$env:Razorpay__KeySecret = "<razorpay-test-secret>"
```

The Razorpay key ID is returned to the checkout client. The key secret must remain on the server. Use Razorpay test credentials until the integration is ready for production.

## Run locally

Start the Order Service first and make sure it trusts the same JWT issuer, audience, and signing key as the API Gateway and Payment Service caller.

```powershell
dotnet restore PaymentService.sln
dotnet ef database update --project .\PaymentService\PaymentService.csproj
dotnet run --project .\PaymentService\PaymentService.csproj --launch-profile https
```

The development profile listens on `https://localhost:7242`. Swagger is available at `/swagger` in Development.

## Checkout API

- `POST /api/payments` accepts an `orderId` and optional `idempotencyKey`. The service fetches the authenticated user's order and uses its stored total; it does not accept client-supplied amounts.
- `GET /api/payments/{id}` returns an owned payment.
- `POST /api/payments/{id}/verify` accepts Razorpay's `razorpay_order_id`, `razorpay_payment_id`, and `razorpay_signature` as `providerOrderId`, `providerPaymentId`, and `signature`. It verifies the signature with HMAC-SHA256 and confirms the order.

All order-service requests forward the caller's bearer token. Route authentication is enforced at the API Gateway.
