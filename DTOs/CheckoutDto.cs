namespace MyBookShopAPI.DTOs
{
    public class CheckoutDto
    {
        // Optional address/payment info placeholder
        public string? ShippingAddress { get; set; }
        public string? PaymentMethod { get; set; } // e.g. "COD", "Card"
    }
}

