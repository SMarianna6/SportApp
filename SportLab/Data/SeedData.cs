using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;

namespace SportLab.Models
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new SportLabContext(
                serviceProvider.GetRequiredService<DbContextOptions<SportLabContext>>()))
            {
         
                if (context.Products.Any())
                {
                    return;   
                }

                context.Products.AddRange(
                    new Product
                    {
                        Name = "Whey Protein Gold",
                        Description = "High-quality protein for muscle recovery and growth",
                        Price = 1299.99m,
                        StockQuantity = 25,
                        PhotoUrl = "/images/protein.jpg"
                    },
                    new Product
                    {
                        Name = "Adjustable Dumbbells",
                        Description = "Set of adjustable dumbbells for home workouts",
                        Price = 3499.00m,
                        StockQuantity = 10,
                        PhotoUrl = "/images/dumbbells.jpg"
                    },
                    new Product
                    {
                        Name = "Yoga Mat Fitness",
                        Description = "Anti-slip mat for yoga, stretching and fitness exercises",
                        Price = 599.50m,
                        StockQuantity = 40,
                        PhotoUrl = "/images/mat.jpg"
                    },
                    new Product
                    {
                        Name = "Sport Shaker Bottle",
                        Description = "Leak-proof shaker bottle for protein shakes and water",
                        Price = 249.00m,
                        StockQuantity = 60,
                        PhotoUrl = "/images/shaker.jpg"
                    }
                );

                context.SaveChanges();
            }
        }
    }
}
