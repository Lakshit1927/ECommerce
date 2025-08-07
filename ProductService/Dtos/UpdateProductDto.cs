using System.ComponentModel.DataAnnotations;

namespace ProductService.Dtos;

public record UpdateProductDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;
    
    [StringLength(500)]
    public string Description { get; init; } = string.Empty;
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; init; }
}
