using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    public class Product
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }

        [Column("Photo")]
        public string? PhotoUrl { get; set; }

        [NotMapped]
        public string? Photo
        {
            get => PhotoUrl;
            set => PhotoUrl = value;
        }

        [NotMapped]
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
    }
}
