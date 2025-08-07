using System.ComponentModel.DataAnnotations;

namespace OrderService.Dtos;

public record UpdateOrderDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string CustomerName { get; init; } = string.Empty;
    
    [Required]
    [StringLength(200, MinimumLength = 5)]
    public string Address { get; init; } = string.Empty;
    
    [Required]
    [MinLength(1, ErrorMessage = "At least one product must be selected")]
    public List<int> ProductIds { get; init; } = new();
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than 0")]
    public decimal TotalAmount { get; init; }
}