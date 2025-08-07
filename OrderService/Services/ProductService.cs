using OrderService.Dtos;
using System.Text.Json;

namespace OrderService.Services;

public class ProductService : IProductService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProductService(HttpClient httpClient, ILogger<ProductService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<ProductDto?> GetProductAsync(int productId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/products/{productId}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ProductDto>(json, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching product {ProductId}", productId);
            throw new InvalidOperationException($"Failed to fetch product {productId}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for product {ProductId}", productId);
            throw new InvalidOperationException($"Failed to parse product {productId} response", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching product {ProductId}", productId);
            throw;
        }
    }

    public async Task<IEnumerable<ProductDto>> GetProductsAsync(IEnumerable<int> productIds)
    {
        try
        {
            var ids = productIds.ToList();
            if (!ids.Any())
            {
                return Enumerable.Empty<ProductDto>();
            }

            // Use batch endpoint to solve N+1 problem
            var idsParam = string.Join(",", ids);
            var response = await _httpClient.GetAsync($"/products/batch?ids={idsParam}");
            
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<IEnumerable<ProductDto>>(json, _jsonOptions) 
                          ?? Enumerable.Empty<ProductDto>();

            // Validate that all requested products were found
            var foundIds = products.Select(p => p.Id).ToHashSet();
            var missingIds = ids.Where(id => !foundIds.Contains(id)).ToList();
            
            if (missingIds.Any())
            {
                _logger.LogWarning("Products not found: {MissingProductIds}", string.Join(", ", missingIds));
                throw new InvalidOperationException($"Products not found: {string.Join(", ", missingIds)}");
            }

            return products;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching products {ProductIds}", string.Join(", ", productIds));
            throw new InvalidOperationException("Failed to fetch products", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for products {ProductIds}", string.Join(", ", productIds));
            throw new InvalidOperationException("Failed to parse products response", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching products {ProductIds}", string.Join(", ", productIds));
            throw;
        }
    }

    public async Task<decimal> CalculateTotalPriceAsync(IEnumerable<int> productIds)
    {
        var products = await GetProductsAsync(productIds);
        return products.Sum(p => p.Price);
    }
}