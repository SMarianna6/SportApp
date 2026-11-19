using System.ComponentModel.DataAnnotations;

namespace SportLab.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Validation.UserRequired")]
        [Display(Name = "Field.User")]
        public int UserId { get; set; }
        public User? User { get; set; }


        [Required]
        [Display(Name = "Field.OrderDate")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = false)]
        public DateTime OrderDate { get; set; }

        [Required(ErrorMessage = "Validation.TotalAmountRequired")]
        [Range(0.01, 1000000.00, ErrorMessage = "Validation.TotalAmountRange")]
        [Display(Name = "Field.TotalAmount")]
        [DataType(DataType.Currency)]
        [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = false)]
        public decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "Validation.StatusRequired")]
        [StringLength(50)]
        [Display(Name = "Field.Status")]
        public required string Status { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
