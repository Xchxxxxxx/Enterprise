using AutoMapper;
using EfCore.Enterprise.Application.Crud;
using EfCore.Enterprise.Domain.Interfaces;
using MyApp.Contracts.News.Dtos;
using MyApp.Domain.Entities;

namespace MyApp.Application.News.Services;

/// <summary>
/// 新闻服务 - 演示 CrudAppService 泛型基类（零代码CRUD）
/// 只需继承 CrudAppService 即可获得完整的分页、增删改查功能
/// </summary>
public class NewsService : CrudAppService<Domain.Entities.News, NewsDto, CreateNewsDto, UpdateNewsDto>
{
    public NewsService(ISuperRepository<Domain.Entities.News> repository, IMapper mapper)
        : base(repository, mapper)
    {
    }
}