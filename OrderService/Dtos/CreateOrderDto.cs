namespace OrderService.Api.Dtos;

public record CreateOrderDto
{
    public string CustomerName { get; init; } = default!;
    public string Address { get; init; } = default!;
    public List<int> ProductIds { get; init; } = new();
    public DateTime OrderDate { get; init; }
}
