using AutoMapper;
using EfCore.Enterprise.Domain.Interfaces;
using FluentValidation;

namespace EfCore.Enterprise.Application.Services;

public abstract class BaseService<TContext>
    where TContext : class
{
    protected readonly IUnitOfWork<TContext> UnitOfWork;
    protected readonly IMapper Mapper;
    protected readonly ICurrentUser CurrentUser;
    protected readonly TContext Context;

    protected BaseService(IUnitOfWork<TContext> unitOfWork, IMapper mapper, ICurrentUser currentUser)
    {
        UnitOfWork = unitOfWork;
        Mapper = mapper;
        CurrentUser = currentUser;
        Context = unitOfWork.Context;
    }
}

public abstract class BaseService<TEntity, TDto, TCreateDto, TUpdateDto, TContext> : BaseService<TContext>
    where TEntity : class
    where TContext : class
{
    protected readonly ISuperRepository<TEntity> Repository;

    protected BaseService(
        ISuperRepository<TEntity> repository,
        IUnitOfWork<TContext> unitOfWork,
        IMapper mapper,
        ICurrentUser currentUser)
        : base(unitOfWork, mapper, currentUser)
    {
        Repository = repository;
    }
}