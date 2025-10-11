namespace MyBookShopAPI.DTOs
{
    public class OrderItemDto
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty; // ✅ Added
        public int Quantity { get; set; }
        public decimal Price { get; set; } // unit price at time of order
    }
}

