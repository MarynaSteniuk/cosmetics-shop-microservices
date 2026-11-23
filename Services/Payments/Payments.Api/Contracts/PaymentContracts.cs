using System.ComponentModel.DataAnnotations;

namespace Payments.Api.Contracts;

// Range(double, double): межі не парсяться з рядка, тому не залежать від культури Windows (uk-UA має кому як роздільник)

public sealed record PaymentDto(
    Guid Id,
    long OrderId,
    decimal Amount,
    string Currency,
    string Status,
    DateTime ProcessedAt,
    string ProcessedBy,
    decimal RefundedAmount,
    DateTime? RefundedAt);

public sealed record ProcessPaymentRequest(
    [Range(1, long.MaxValue)] long OrderId,
    [Range(0.01, 1_000_000)] decimal Amount,
    [StringLength(3, MinimumLength = 3)] string Currency = "UAH");

public sealed record RefundPaymentRequest(
    [Required] Guid PaymentId,
    [Range(0.01, 1_000_000)] decimal? Amount,
    [MaxLength(300)] string? Reason);
