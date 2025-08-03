namespace ProductService.Dtos;

public record UpdateProductDto(
    string Name,
    string Description,
    decimal Price
);
