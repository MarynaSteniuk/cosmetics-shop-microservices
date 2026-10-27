using System.Data;
using System.Diagnostics;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Orders.Bll.Dtos;
using Orders.Bll.Telemetry;
using Orders.Dal.Interfaces;
using Orders.Domain;
using Orders.Domain.Exceptions;
using Orders.Domain.Models;

namespace Orders.Bll.Services;

public sealed class OrderService : IOrderService
{
    private const string Actor = "orders-api";

    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IUnitOfWork uow, IMapper mapper, ILogger<OrderService> logger)
    {
        _uow = uow;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<OrderDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var order = await _uow.Orders.GetWithItemsAsync(id, ct)
            ?? throw new NotFoundException($"Order {id} not found");
        return _mapper.Map<OrderDto>(order);
    }

    public async Task<IReadOnlyList<OrderSummaryDto>> GetByCustomerAsync(long customerId, string? status,
        CancellationToken ct = default)
    {
        if (status is not null && !OrderStatuses.All.Contains(status))
            throw new ValidationException($"Unknown order status '{status}'");

        _ = await _uow.Customers.GetByIdAsync(customerId, ct)
            ?? throw new NotFoundException($"Customer {customerId} not found");

        var orders = await _uow.Orders.GetByCustomerAsync(customerId, status, ct);
        return _mapper.Map<List<OrderSummaryDto>>(orders);
    }

    public async Task<OrderDto> CreateAsync(CreateOrderDto input, CancellationToken ct = default)
    {
        using var activity = OrdersTelemetry.Source.StartActivity("CreateOrder");
        activity?.SetTag("customer.id", input.CustomerId);

        var lines = input.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Units = g.Sum(i => i.Units) })
            .ToList();

        long orderId;

        await _uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            _ = await _uow.Customers.GetByIdAsync(input.CustomerId, ct)
                ?? throw new NotFoundException($"Customer {input.CustomerId} not found");

            PromoCode? promo = null;
            if (!string.IsNullOrWhiteSpace(input.PromoCode))
            {
                promo = await _uow.Orders.FindActivePromoCodeAsync(input.PromoCode.Trim(), ct)
                    ?? throw new ValidationException($"Promo code '{input.PromoCode}' is invalid or expired");
            }

            orderId = await _uow.Orders.AddAsync(new Order
            {
                CustomerId = input.CustomerId,
                PromoCodeId = promo?.Id,
                Status = OrderStatuses.Submitted,
                CreatedBy = Actor
            }, ct);

            await _uow.Orders.AddDetailsAsync(new OrderDetails
            {
                OrderId = orderId,
                ShippingAddress = input.ShippingAddress.Trim(),
                City = input.City.Trim(),
                Comment = input.Comment
            }, ct);

            await _uow.Orders.AddStatusHistoryAsync(orderId, null, OrderStatuses.Submitted, Actor, ct);

            var products = await _uow.Products.GetByIdsAsync(lines.Select(l => l.ProductId), ct);
            var percent = promo?.DiscountPercent ?? 0m;
            var total = 0m;

            foreach (var line in lines)
            {
                var product = products.FirstOrDefault(p => p.Id == line.ProductId)
                    ?? throw new NotFoundException($"Product {line.ProductId} not found");

                if (product.Stock < line.Units)
                    throw new BusinessConflictException(
                        $"Not enough stock for product {product.Id}: requested {line.Units}, available {product.Stock}");

                var discount = Math.Round(product.Price * percent / 100m, 2, MidpointRounding.AwayFromZero);

                await _uow.Orders.AddItemAsync(new OrderItem
                {
                    OrderId = orderId,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Discount = discount,
                    Units = line.Units
                }, ct);

                total += (product.Price - discount) * line.Units;
            }

            await _uow.Orders.UpdateAsync(new Order { Id = orderId, TotalAmount = total, UpdatedBy = Actor }, ct);
            await _uow.CommitAsync(ct);

            activity?.SetTag("order.id", orderId);
            activity?.SetTag("order.items_count", lines.Count);
            activity?.SetTag("order.total", total);

            _logger.LogInformation(
                "Order {OrderId} created for customer {CustomerId}: {ItemsCount} items, amount {Amount}, status {Status}",
                orderId, input.CustomerId, lines.Count, total, OrderStatuses.Submitted);
        }
        catch (Exception ex)
        {
            await _uow.RollbackAsync(ct);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }

        return await GetByIdAsync(orderId, ct);
    }

    public async Task ConfirmAsync(long id, CancellationToken ct = default)
    {
        using var activity = OrdersTelemetry.Source.StartActivity("ConfirmOrder");
        activity?.SetTag("order.id", id);

        var itemsCount = 0;

        await _uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            var order = await _uow.Orders.GetWithItemsAsync(id, ct)
                ?? throw new NotFoundException($"Order {id} not found");

            if (order.Items.Count == 0)
                throw new BusinessConflictException($"Order {id} has no items");

            itemsCount = order.Items.Count;

            await _uow.Orders.ConfirmAsync(id, Actor, ct);

            foreach (var item in order.Items)
                await _uow.Products.DecreaseStockAsync(item.ProductId, item.Units, ct);

            await _uow.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await _uow.RollbackAsync(ct);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }

        activity?.SetTag("order.items_count", itemsCount);
        _logger.LogInformation("Order {OrderId} confirmed, {ItemsCount} items, status {Status}",
            id, itemsCount, OrderStatuses.StockConfirmed);
    }

    public async Task<OrderDto> ChangeStatusAsync(long id, ChangeOrderStatusDto input, CancellationToken ct = default)
    {
        var status = input.Status.Trim();
        if (!OrderStatuses.All.Contains(status))
            throw new ValidationException($"Unknown order status '{input.Status}'");

        await _uow.Orders.ChangeStatusAsync(id, status, Actor, ct);
        _logger.LogInformation("Order {OrderId} moved to {Status}", id, status);

        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        await _uow.Orders.DeleteAsync(id, ct);
        _logger.LogInformation("Order {OrderId} deleted", id);
    }
}
