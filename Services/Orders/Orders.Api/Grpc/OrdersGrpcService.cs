using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Orders.Bll.Dtos;
using Orders.Bll.Services;
using Orders.Domain.Exceptions;
using Orders.Grpc;

namespace Orders.Api.Grpc;

public sealed class OrdersGrpcService : OrdersService.OrdersServiceBase
{
    private readonly IOrderService _orders;

    public OrdersGrpcService(IOrderService orders)
    {
        _orders = orders;
    }

    public override async Task<OrderReply> GetOrder(GetOrderRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Order id must be positive"));

        try
        {
            var order = await _orders.GetByIdAsync(request.Id, context.CancellationToken);
            return ToReply(order);
        }
        catch (NotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (ValidationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    private static OrderReply ToReply(OrderDto order)
    {
        var reply = new OrderReply
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            Status = order.Status,
            OrderDate = Timestamp.FromDateTime(ToUtc(order.OrderDate)),
            TotalAmount = (double)order.TotalAmount,
            City = order.City ?? ""
        };

        reply.Items.AddRange(order.Items.Select(item => new OrderItemReply
        {
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            UnitPrice = (double)item.UnitPrice,
            Discount = (double)item.Discount,
            Units = item.Units,
            LineTotal = (double)item.LineTotal
        }));

        return reply;
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
