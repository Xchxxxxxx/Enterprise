using AutoMapper;
using MyApp.Contracts.Categories.Dtos;
using MyApp.Domain.Entities;
using EfCore.Enterprise.Application.Mapping;

namespace MyApp.Application.Categories.Mapping;

public class CategoryProfile : BaseProfile
{
    protected override void Configure()
    {
        CreateMap<Category, CategoryDto>();
        CreateMap<CreateCategoryDto, Category>();
    }
}