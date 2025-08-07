using OrderService.Dtos;

namespace OrderService.Services;

public interface IProductService
{
    Task<ProductDto?> GetProductAsync(int productId);
    Task<IEnumerable<ProductDto>> GetProductsAsync(IEnumerable<int> productIds);
    Task<decimal> CalculateTotalPriceAsync(IEnumerable<int> productIds);
}