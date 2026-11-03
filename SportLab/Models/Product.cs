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

        [Display(Name = "Category")]
        public string? Category { get; set; }

        [Display(Name = "Rating")]
        public string? Rating { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
