using BookingService.Models;

namespace BookingService.PaymentMock;

public class MockPaymentResult
{
    public bool IsSuccess { get; init; }

    public string? ProviderTransactionId { get; init; }
}

public class MockPaymentService
{
    public MockPaymentResult ProcessPayment(decimal amount, string? mockPaymentHeader)
    {
        var mode = string.IsNullOrWhiteSpace(mockPaymentHeader) ? "success" : mockPaymentHeader.Trim();

        if (string.Equals(mode, "fail", StringComparison.OrdinalIgnoreCase))
        {
            return new MockPaymentResult
            {
                IsSuccess = false,
                ProviderTransactionId = null
            };
        }

        return new MockPaymentResult
        {
            IsSuccess = true,
            ProviderTransactionId = Guid.NewGuid().ToString()
        };
    }
}
