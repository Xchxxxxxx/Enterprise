namespace MyApp.Contracts.Products.Dtos;

public class UpdateProductPriceDto
{
    public long Id { get; set; }
    public decimal NewPrice { get; set; }
}