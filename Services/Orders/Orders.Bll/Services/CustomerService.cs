using AutoMapper;
using Microsoft.Extensions.Logging;
using Orders.Bll.Dtos;
using Orders.Dal.Interfaces;
using Orders.Domain.Exceptions;
using Orders.Domain.Models;

namespace Orders.Bll.Services;

public sealed class CustomerService : ICustomerService
{
    private const string Actor = "orders-api";

    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(IUnitOfWork uow, IMapper mapper, ILogger<CustomerService> logger)
    {
        _uow = uow;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CustomerDto>> GetPageAsync(int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1)
            throw new ValidationException("Page must be greater than 0");
        if (pageSize is < 1 or > 100)
            throw new ValidationException("Page size must be between 1 and 100");

        var customers = await _uow.Customers.GetPageAsync(page, pageSize, ct);
        return _mapper.Map<List<CustomerDto>>(customers);
    }

    public async Task<CustomerDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var customer = await _uow.Customers.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Customer {id} not found");
        return _mapper.Map<CustomerDto>(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto input, CancellationToken ct = default)
    {
        var email = input.Email.Trim();
        if (await _uow.Customers.GetByEmailAsync(email, ct) is not null)
            throw new BusinessConflictException($"Customer with email {email} already exists");

        var customer = _mapper.Map<Customer>(input);
        customer.Email = email;
        customer.FullName = input.FullName.Trim();
        customer.CreatedBy = Actor;

        var id = await _uow.Customers.AddAsync(customer, ct);
        _logger.LogInformation("Customer {CustomerId} created", id);

        return await GetByIdAsync(id, ct);
    }

    public async Task<CustomerDto> UpdateAsync(long id, UpdateCustomerDto input, CancellationToken ct = default)
    {
        var customer = await _uow.Customers.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Customer {id} not found");

        var email = input.Email.Trim();
        var sameEmail = await _uow.Customers.GetByEmailAsync(email, ct);
        if (sameEmail is not null && sameEmail.Id != id)
            throw new BusinessConflictException($"Customer with email {email} already exists");

        _mapper.Map(input, customer);
        customer.Email = email;
        customer.FullName = input.FullName.Trim();
        customer.UpdatedBy = Actor;

        await _uow.Customers.UpdateAsync(customer, ct);
        _logger.LogInformation("Customer {CustomerId} updated", id);

        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        if (!await _uow.Customers.SoftDeleteAsync(id, Actor, ct))
            throw new NotFoundException($"Customer {id} not found");

        _logger.LogInformation("Customer {CustomerId} deleted", id);
    }
}
