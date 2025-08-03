namespace ProductService.Dtos;

public record CreateProductDto(
    string Name,
    string Genre,
    decimal Price,
    DateOnly ReleaseDate,
    string Description
);
