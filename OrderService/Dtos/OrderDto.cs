namespace OrderService.Dtos;

public record UpdateOrderDto(
    string CustomerName,
    string Address,
    List<int> ProductIds,
    decimal TotalAmount
);
