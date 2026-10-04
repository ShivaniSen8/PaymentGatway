# Payment Service

ASP.NET Core 8 payment API using Razorpay Orders and server-side checkout signature verification.

## Configuration

Development uses an in-memory database and a mock payment gateway. These defaults let the local checkout flow run without SQL or Razorpay credentials. Mock payments are for local development only.

For Razorpay and a persistent SQL Server database, provide configuration through environment variables. Do not commit API keys or database credentials.

```powershell
$env:ConnectionStrings__PaymentDb = "<sql-server-connection-string>"
$env:OrderService__BaseUrl = "http://localhost:5280/"
$env:Razorpay__KeyId = "rzp_test_<key-id>"
$env:Razorpay__KeySecret = "<razorpay-test-secret>"
$env:PaymentGateway__UseMock = "false"
```

The Razorpay key ID is returned to the checkout client. The key secret must remain on the server. Use Razorpay test credentials until the integration is ready for production.

## Run locally

Start the Order Service on port 5280 and the API Gateway on port 5098 first. Authenticated storefront checkout creates an order through the gateway, then initializes payment through `/api/payments`.

```sh
dotnet restore PaymentService/PaymentService.csproj
dotnet run --project PaymentService/PaymentService.csproj --urls http://localhost:5270
```

In Development, the service listens on `http://localhost:5270`. Swagger is available at `/swagger`. For a persistent database, set `ConnectionStrings__PaymentDb` and apply the EF migrations before starting outside Development.

## Checkout API

- `POST /api/payments` accepts an `orderId` and optional `idempotencyKey`. The service fetches the authenticated user's order and uses its stored total; it does not accept client-supplied amounts.
- `GET /api/payments/{id}` returns an owned payment.
- `POST /api/payments/{id}/verify` accepts Razorpay's `razorpay_order_id`, `razorpay_payment_id`, and `razorpay_signature` as `providerOrderId`, `providerPaymentId`, and `signature`. It verifies the signature with HMAC-SHA256 and confirms the order.

All order-service requests forward the caller's bearer token. Route authentication is enforced at the API Gateway.
