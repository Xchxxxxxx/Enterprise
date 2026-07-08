using MyApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MyApp.Infrastructure.Data.SeedData;

public static class ProductSeedData
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        var product1 = new Product("示例产品A", 99.99m, 100);
        typeof(Product).GetProperty("Id")?.SetValue(product1, 1L);
        typeof(Product).GetProperty("CreatedTime")?.SetValue(product1, new DateTime(2025, 1, 1));

        var product2 = new Product("示例产品B", 199.99m, 50);
        typeof(Product).GetProperty("Id")?.SetValue(product2, 2L);
        typeof(Product).GetProperty("CreatedTime")?.SetValue(product2, new DateTime(2025, 1, 2));

        var product3 = new Product("示例产品C", 299.99m, 20);
        typeof(Product).GetProperty("Id")?.SetValue(product3, 3L);
        typeof(Product).GetProperty("CreatedTime")?.SetValue(product3, new DateTime(2025, 1, 3));

        modelBuilder.Entity<Product>().HasData(product1, product2, product3);
    }
}