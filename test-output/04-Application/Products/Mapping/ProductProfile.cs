using AutoMapper;
using MyApp.Contracts.Products.Dtos;
using MyApp.Domain.Entities;
using EfCore.Enterprise.Application.Mapping;

namespace MyApp.Application.Products.Mapping;

public class ProductProfile : BaseProfile
{
  
    protected override void Configure()
    {
        CreateMap<Product, ProductDto>();
        CreateMap<CreateProductDto, Product>();
    }
}