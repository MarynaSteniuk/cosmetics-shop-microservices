using Microsoft.AspNetCore.Mvc;
using Payments.Api.Authorization;
using Payments.Api.Contracts;
using Payments.Api.Services;
using Platform.ServiceDefaults.Security;

namespace Payments.Api.Controllers;

/// <summary>Лаб. 09, таблиця 3.3.3: усі операції з платежами доступні лише ролі admin.</summary>
[ApiController]
[Route("api/payments")]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentStore _store;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentStore store, ILogger<PaymentsController> logger)
    {
        _store = store;
        _logger = logger;
    }

    [HttpGet]
    [RequirePermission(Permissions.Payments.Read)]
    [ProducesResponseType<IReadOnlyList<PaymentDto>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<PaymentDto>> GetAll([FromQuery] long? orderId)
        => Ok(_store.GetAll(orderId));

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.Payments.Read)]
    [ProducesResponseType<PaymentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<PaymentDto> GetById(Guid id)
        => _store.Find(id) is { } payment
            ? Ok(payment)
            : Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: $"Payment {id} not found");

    [HttpPost("process")]
    [RequirePermission(Permissions.Payments.Process)]
    [ProducesResponseType<PaymentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<PaymentDto> Process(ProcessPaymentRequest request)
    {
        var actor = User.FindFirst(PlatformAuthentication.SubjectClaimType)?.Value ?? "unknown";
        var payment = _store.Process(request, actor);

        _logger.LogInformation("Payment {PaymentId} for order {OrderId}: {Amount} {Currency} processed by {Subject}",
            payment.Id, payment.OrderId, payment.Amount, payment.Currency, actor);

        return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
    }

    [HttpPost("refund")]
    [RequirePermission(Permissions.Payments.Refund)]
    [ProducesResponseType<PaymentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public ActionResult<PaymentDto> Refund(RefundPaymentRequest request)
    {
        if (_store.Find(request.PaymentId) is not { } payment)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found",
                detail: $"Payment {request.PaymentId} not found");

        var amount = request.Amount ?? payment.Amount - payment.RefundedAmount;
        if (amount <= 0 || payment.RefundedAmount + amount > payment.Amount)
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflict",
                detail: "Refund amount exceeds the paid amount");

        var refunded = _store.Refund(payment, amount);
        _logger.LogInformation("Payment {PaymentId} refunded {Amount}; reason: {Reason}",
            payment.Id, amount, request.Reason ?? "-");

        return Ok(refunded);
    }
}
