using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Mapping;

public class CatalogMappingProfile : Profile
{
    public CatalogMappingProfile()
    {
        CreateMap<Product, ProductListItemDto>()
            .ForMember(d => d.BrandName, o => o.MapFrom(s => s.Brand.Name));

        CreateMap<Product, ProductDto>()
            .ForMember(d => d.BrandName, o => o.MapFrom(s => s.Brand.Name))
            .ForMember(d => d.Categories, o => o.MapFrom(s => s.ProductCategories));

        CreateMap<ProductDetail, ProductDetailDto>();
        CreateMap<ProductImage, ProductImageDto>();

        CreateMap<ProductCategory, ProductCategoryDto>()
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Category.Name))
            .ForMember(d => d.Slug, o => o.MapFrom(s => s.Category.Slug));

        CreateMap<Category, CategoryDto>();

        CreateMap<Category, CategoryWithProductsDto>()
            .ForMember(d => d.Products, o => o.MapFrom(s => s.ProductCategories));

        CreateMap<ProductCategory, ProductInCategoryDto>()
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.Sku, o => o.MapFrom(s => s.Product.Sku))
            .ForMember(d => d.Price, o => o.MapFrom(s => s.Product.Price));

        CreateMap<ProductCategory, RecentlyAddedProductDto>()
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.Sku, o => o.MapFrom(s => s.Product.Sku))
            .ForMember(d => d.BrandName, o => o.MapFrom(s => s.Product.Brand.Name))
            .ForMember(d => d.Price, o => o.MapFrom(s => s.Product.Price))
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name));
    }
}
