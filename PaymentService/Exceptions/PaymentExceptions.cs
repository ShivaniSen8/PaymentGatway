namespace PaymentService.Exceptions;

public sealed class PaymentProviderException : Exception
{
    public PaymentProviderException(string message)
        : base(message)
    {
    }

    public PaymentProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class PaymentVerificationException : Exception
{
    public PaymentVerificationException(string message)
        : base(message)
    {
    }
}

public sealed class OrderServiceException : Exception
{
    public OrderServiceException(string message)
        : base(message)
    {
    }

    public OrderServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
