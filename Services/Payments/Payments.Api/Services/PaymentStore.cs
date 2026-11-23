using System.Collections.Concurrent;
using Payments.Api.Contracts;

namespace Payments.Api.Services;

public interface IPaymentStore
{
    IReadOnlyList<PaymentDto> GetAll(long? orderId);
    PaymentDto? Find(Guid id);
    PaymentDto Process(ProcessPaymentRequest request, string processedBy);
    PaymentDto Refund(PaymentDto payment, decimal amount);
}

/// <summary>Лаб. 09: заглушка платіжного сервісу — платежі зберігаються в пам'яті, гроші ніде не списуються.</summary>
public sealed class InMemoryPaymentStore : IPaymentStore
{
    private readonly ConcurrentDictionary<Guid, PaymentDto> _payments = new();

    public IReadOnlyList<PaymentDto> GetAll(long? orderId)
        => _payments.Values
            .Where(p => orderId is null || p.OrderId == orderId)
            .OrderByDescending(p => p.ProcessedAt)
            .ToList();

    public PaymentDto? Find(Guid id) => _payments.GetValueOrDefault(id);

    public PaymentDto Process(ProcessPaymentRequest request, string processedBy)
    {
        var payment = new PaymentDto(Guid.NewGuid(), request.OrderId, request.Amount,
            request.Currency.ToUpperInvariant(), "Paid", DateTime.UtcNow, processedBy, 0m, null);
        _payments[payment.Id] = payment;
        return payment;
    }

    public PaymentDto Refund(PaymentDto payment, decimal amount)
    {
        var refunded = payment.RefundedAmount + amount;
        var updated = payment with
        {
            RefundedAmount = refunded,
            RefundedAt = DateTime.UtcNow,
            Status = refunded >= payment.Amount ? "Refunded" : "PartiallyRefunded"
        };
        _payments[payment.Id] = updated;
        return updated;
    }
}
