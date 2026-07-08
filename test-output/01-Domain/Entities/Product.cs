using EfCore.Enterprise.Domain.Entities;
using MyApp.Domain.Events;

namespace MyApp.Domain.Entities;

public class Product : BaseFullEntity<long>
{
    private Product() { }

    public Product(string name, decimal price, int stock)
    {
        Name = name;
        Price = price;
        Stock = stock;
    }

    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public string? Description { get; private set; }

    public void ChangePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(newPrice), "价格必须大于0");

        Price = newPrice;
        AddDomainEvent(new ProductPriceChangedDomainEvent(this, newPrice));
    }

    public void ChangeStock(int newStock)
    {
        if (newStock < 0)
            throw new ArgumentOutOfRangeException(nameof(newStock), "库存不能为负");

        Stock = newStock;
    }

    public void ChangeDescription(string? description)
    {
        Description = description;
    }
}