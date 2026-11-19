using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Validation.ProductNameRequired")]
        [Display(Name = "Field.ProductName")]
        public required string Name { get; set; }

        [Display(Name = "Field.Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Validation.ProductPriceRequired")]
        [Display(Name = "Field.Price")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = false)]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Validation.StockQuantityRequired")]
        [Display(Name = "Field.StockQuantity")]
        public int StockQuantity { get; set; }

        [Column("Photo")]
        [Display(Name = "Field.PhotoUrl")]
        public string? PhotoUrl { get; set; }

        [NotMapped]
        public string? Photo
        {
            get => PhotoUrl;
            set => PhotoUrl = value;
        }

        [Display(Name = "Field.Category")]
        public string? Category { get; set; }

        [Display(Name = "Field.Rating")]
        public string? Rating { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
