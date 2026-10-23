using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [Display(Name = "Product Name")]
        public required string Name { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Display(Name = "Price")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock quantity is required")]
        [Display(Name = "Quantity in Stock")]
        public int StockQuantity { get; set; }

        [Column("Photo")]
        [Display(Name = "Photo URL")]
        public string? PhotoUrl { get; set; }

        [NotMapped]
        public string? Photo
        {
            get => PhotoUrl;
            set => PhotoUrl = value;
        }

        [NotMapped]
        [Display(Name = "Category")]
        public string Category
        {
            get
            {
                var text = $"{Name} {Description}".ToLowerInvariant();
                if (text.Contains("protein") || text.Contains("bcaa") || text.Contains("creatine") || text.Contains("vitamin") || text.Contains("amino") || text.Contains("nutrition") || text.Contains("bar") || text.Contains("snack"))
                    return "Nutrition";
                if (text.Contains("shaker") || text.Contains("bottle") || text.Contains("towel") || text.Contains("strap") || text.Contains("lock") || text.Contains("accessory") || text.Contains("accessories"))
                    return "Accessories";
                if (text.Contains("bag") || text.Contains("shirt") || text.Contains("hoodie") || text.Contains("shorts") || text.Contains("short") || text.Contains("glove") || text.Contains("wear") || text.Contains("apparel"))
                    return "Apparel";
                if (text.Contains("dumbbell") || text.Contains("band") || text.Contains("mat") || text.Contains("belt") || text.Contains("roller") || text.Contains("equipment"))
                    return "Equipment";
                return "Accessories";
            }
        }

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
