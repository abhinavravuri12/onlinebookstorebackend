using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyBookShopAPI.Models
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        public int Id { get; set; }

        // ✅ Foreign key for User
        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        // ✅ Date & Status
        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Pending";

        // ✅ Added fields for checkout
        [Required, MaxLength(255)]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        // ✅ Navigation property for related order items
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
