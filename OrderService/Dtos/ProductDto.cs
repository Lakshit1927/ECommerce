namespace OrderService.Dtos;

public record ProductDto(
    int Id,
    string Name,
    string Genre,
    decimal Price,
    DateOnly ReleaseDate,
    string Description
);
