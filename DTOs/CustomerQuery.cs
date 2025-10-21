using System;
using System.ComponentModel.DataAnnotations;

namespace MyBookShopAPI.Models
{
    public class CustomerQuery
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [Required]
        public string Subject { get; set; } = string.Empty;



        [Required]
        public string Message { get; set; } = string.Empty;

        public string? AdminReply { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RepliedAt { get; set; }
    }
}
