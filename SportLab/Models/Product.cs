using System.ComponentModel.DataAnnotations;

namespace SportLab.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public string PhotoUrl { get; set; }
    }
}