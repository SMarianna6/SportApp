using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }

        [Display(Name = "Field.Order")]
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        [Display(Name = "Field.Product")]
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Required(ErrorMessage = "Validation.QuantityRequired")]
        [Display(Name = "Field.Quantity")]
        [Range(1, 1000, ErrorMessage = "Validation.QuantityRange")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Validation.UnitPriceRequired")]
        [Display(Name = "Field.UnitPrice")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = false)]
        public decimal UnitPrice { get; set; }
    }
}
