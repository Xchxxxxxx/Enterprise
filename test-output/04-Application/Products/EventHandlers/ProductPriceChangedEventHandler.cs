using MediatR;
using MyApp.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MyApp.Application.Products.EventHandlers;

public class ProductPriceChangedEventHandler : INotificationHandler<ProductPriceChangedDomainEvent>
{
    private readonly ILogger<ProductPriceChangedEventHandler> _logger;

    public ProductPriceChangedEventHandler(ILogger<ProductPriceChangedEventHandler> logger)
    {
        _logger = logger;
    }
    
    public Task Handle(ProductPriceChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "产品 {ProductId} 价格从 {OldPrice} 变更为 {NewPrice}",
            notification.ProductId,
            notification.OldPrice,
            notification.NewPrice);

        return Task.CompletedTask;
    }
}