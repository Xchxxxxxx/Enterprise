using AutoMapper;
using EfCore.Enterprise.Domain.Interfaces;
using FluentValidation;

namespace EfCore.Enterprise.Application.Services;

public abstract class BaseService
{
    protected readonly IUnitOfWork UnitOfWork;
    protected readonly IMapper Mapper;
    protected readonly ICurrentUser CurrentUser;

    protected BaseService(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUser currentUser)
    {
        UnitOfWork = unitOfWork;
        Mapper = mapper;
        CurrentUser = currentUser;
    }
}

public abstract class BaseService<TEntity, TDto, TCreateDto, TUpdateDto> : BaseService
    where TEntity : class
{
    protected readonly ISuperRepository<TEntity> Repository;

    protected BaseService(
        ISuperRepository<TEntity> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUser currentUser)
        : base(unitOfWork, mapper, currentUser)
    {
        Repository = repository;
    }
}