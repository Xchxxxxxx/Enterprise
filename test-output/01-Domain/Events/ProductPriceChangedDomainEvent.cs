using MyApp.Domain.Entities;
using EfCore.Enterprise.Domain.Events;

namespace MyApp.Domain.Events;

public class ProductPriceChangedDomainEvent : DomainEvent
{
    public long ProductId { get; }
    public decimal OldPrice { get; }
    public decimal NewPrice { get; }

    public ProductPriceChangedDomainEvent(Product product, decimal newPrice)
    {
        ProductId = product.Id;
        OldPrice = product.Price;
        NewPrice = newPrice;
    }
}