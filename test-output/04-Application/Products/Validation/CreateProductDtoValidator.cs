using MyApp.Contracts.Products.Dtos;
using FluentValidation;

namespace MyApp.Application.Products.Validation;

/// <summary>
/// 创建产品验证器 - 演示 FluentValidation
/// </summary>
public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("产品名称不能为空")
            .MaximumLength(200).WithMessage("产品名称不能超过200个字符");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("价格必须大于0");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("库存不能为负数");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("描述不能超过1000个字符");
    }
}