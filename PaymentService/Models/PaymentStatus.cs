namespace PaymentService.Models;

public enum PaymentStatus
{
    Created = 1,
    Pending = 2,
    Processing = 3,
    Succeeded = 4,
    Failed = 5,
    Cancelled = 6,
    RefundRequested = 7,
    Refunded = 8
}