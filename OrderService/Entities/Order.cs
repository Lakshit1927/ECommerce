namespace OrderService.Entities
{
    public class Order
    {
        public int Id { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public List<int> ProductIds { get; set; } = new();

        public decimal TotalAmount { get; set; }

        public DateTime OrderDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
