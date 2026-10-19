using AutoMapper;
using Orders.Bll.Dtos;
using Orders.Domain.Models;

namespace Orders.Bll.Mapping;

public class OrdersMappingProfile : Profile
{
    public OrdersMappingProfile()
    {
        CreateMap<Customer, CustomerDto>();

        CreateMap<CreateCustomerDto, Customer>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.Ignore());

        CreateMap<UpdateCustomerDto, Customer>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.Ignore());

        CreateMap<Product, ProductDto>();

        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(d => d.LineTotal, o => o.MapFrom(s => (s.UnitPrice - s.Discount) * s.Units));

        CreateMap<Order, OrderSummaryDto>();

        CreateMap<Order, OrderDto>()
            .ForMember(d => d.ShippingAddress, o => o.MapFrom(s => s.Details!.ShippingAddress))
            .ForMember(d => d.City, o => o.MapFrom(s => s.Details!.City))
            .ForMember(d => d.Comment, o => o.MapFrom(s => s.Details!.Comment));
    }
}
