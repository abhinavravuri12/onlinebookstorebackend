using Microsoft.EntityFrameworkCore;
using MyBookShopAPI.Models;
namespace MyBookShopAPI.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedDatabase(ApplicationDbContext context)
        {
            
            await context.Database.EnsureCreatedAsync();
            
            if (context.Users.Any())
            {
                return; 
            }
           
            await SeedUsers(context);
          
            await SeedBooks(context);
           
            await SeedOrders(context);
            
            await SeedCartItems(context);
        }
        private static async Task SeedUsers(ApplicationDbContext context)
        {
            var users = new List<User>
           {
               new User
               {
                   Username = "democustomer",
                   Email = "demo@customer.com",
                   PasswordHash = BCrypt.Net.BCrypt.HashPassword("demo123"),
                   Role = "Customer",
                   LastLogin = DateTime.UtcNow.AddDays(-1)
               },
               new User
               {
                   Username = "demoadmin",
                   Email = "admin@demo.com",
                   PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                   Role = "Admin",
                   LastLogin = DateTime.UtcNow.AddHours(-2)
               }
           };
            context.Users.AddRange(users);
            await context.SaveChangesAsync();
        }
        private static async Task SeedBooks(ApplicationDbContext context)
        {
            var books = new List<Book>
           {
               new Book
               {
                   Title = "The Great Gatsby",
                   Author = "F. Scott Fitzgerald",
                   Genre = "Fiction",
                   Price = 299.99m,
                   StockQuantity = 50,
                   ImageUrl = "https://images.unsplash.com/photo-1544947950-fa07a98d237f?w=300"
               },
               new Book
               {
                   Title = "To Kill a Mockingbird",
                   Author = "Harper Lee",
                   Genre = "Fiction",
                   Price = 14.99m,
                   StockQuantity = 30,
                   ImageUrl = "https://images.unsplash.com/photo-1481627834876-b7833e8f5570?w=300"
               },
               new Book
               {
                   Title = "Pride and Prejudice",
                   Author = "Jane Austen",
                   Genre = "Romance",
                   Price = 11.99m,
                   StockQuantity = 40,
                   ImageUrl = "https://images.unsplash.com/photo-1512820790803-83ca734da794?w=300"
               },
               new Book
               {
                   Title = "Lord of the Flies",
                   Author = "William Golding",
                   Genre = "Fiction",
                   Price = 12.99m,
                   StockQuantity = 35,
                   ImageUrl = "https://images.unsplash.com/photo-1544947950-fa07a98d237f?w=300"
               },
               new Book
               {
                   Title = "The Hobbit",
                   Author = "J.R.R. Tolkien",
                   Genre = "Fantasy",
                   Price = 16.99m,
                   StockQuantity = 60,
                   ImageUrl = "https://images.unsplash.com/photo-1481627834876-b7833e8f5570?w=300"
               },
               new Book
               {
                   Title = "Harry Potter and the Sorcerer's Stone",
                   Author = "J.K. Rowling",
                   Genre = "Fantasy",
                   Price = 18.99m,
                   StockQuantity = 100,
                   ImageUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=300"
               },
               new Book
               {
                   Title = "The Chronicles of Narnia",
                   Author = "C.S. Lewis",
                   Genre = "Fantasy",
                   Price = 17.99m,
                   StockQuantity = 45,
                   ImageUrl = "https://images.unsplash.com/photo-1512820790803-83ca734da794?w=300"
               }
           };
            context.Books.AddRange(books);
            await context.SaveChangesAsync();
        }
        private static async Task SeedOrders(ApplicationDbContext context)
        {
            var users = await context.Users.ToListAsync();
            var books = await context.Books.ToListAsync();
            var orders = new List<Order>
           {
               new Order
               {
                   UserId = users.First(u => u.Username == "democustomer").Id,
                   OrderDate = DateTime.UtcNow.AddDays(-5),
                   Status = "Completed",
                   ShippingAddress = "123 Main St, Anytown, USA 12345",
                   PaymentMethod = "Credit Card"
               },
           };
            context.Orders.AddRange(orders);
            await context.SaveChangesAsync();
           
            var orderItems = new List<OrderItem>
           {
               new OrderItem
               {
                   OrderId = orders[0].Id,
                   BookId = books[0].Id, 
                   Quantity = 2,
                   Price = books[0].Price
               },
               new OrderItem
               {
                   OrderId = orders[0].Id,
                   BookId = books[1].Id, 
                   Quantity = 1,
                   Price = books[1].Price
               },
               new OrderItem
               {
                   OrderId = orders[1].Id,
                   BookId = books[2].Id, 
                   Quantity = 1,
                   Price = books[2].Price
               },
               new OrderItem
               {
                   OrderId = orders[1].Id,
                   BookId = books[6].Id, 
                   Quantity = 1,
                   Price = books[6].Price
               },
               new OrderItem
               {
                   OrderId = orders[2].Id,
                   BookId = books[7].Id, 
                   Quantity = 3,
                   Price = books[7].Price
               }
           };
            context.OrderItems.AddRange(orderItems);
            await context.SaveChangesAsync();
        }
        private static async Task SeedCartItems(ApplicationDbContext context)
        {
            var users = await context.Users.ToListAsync();
            var books = await context.Books.ToListAsync();
            var cartItems = new List<CartItem>
           {
               new CartItem
               {
                   UserId = users.First(u => u.Username == "democustomer").Id,
                   BookId = books[3].Id, 
                   Quantity = 1
               },
               new CartItem
               {
                   UserId = users.First(u => u.Username == "democustomer").Id,
                   BookId = books[4].Id, 
                   Quantity = 2
               },
               new CartItem
               {
                   UserId = users.First(u => u.Username == "john_doe").Id,
                   BookId = books[8].Id, 
                   Quantity = 1
               }
           };
            context.CartItems.AddRange(cartItems);
            await context.SaveChangesAsync();
        }
    }
}
