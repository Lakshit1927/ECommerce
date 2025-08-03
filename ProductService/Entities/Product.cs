namespace Productservice.Entities;

public class Product
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Genre { get; set; }

    public required decimal Price { get; set; }

    public required DateOnly ReleaseDate { get; set; }

    public required string Description { get; set; }
}
