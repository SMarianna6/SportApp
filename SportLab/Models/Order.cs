using System.ComponentModel.DataAnnotations;

namespace SportLab.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "User is required")]
        [Display(Name = "User")]
        public int UserId { get; set; }
        public User? User { get; set; }


        [Required]
        [Display(Name = "Order Date")]
        [DataType(DataType.DateTime)]
        public DateTime OrderDate { get; set; }

        [Required(ErrorMessage = "Total amount is required")]
        [Range(0.01, 1000000.00, ErrorMessage = "Total amount must be greater than 0")]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [StringLength(50)]
        [Display(Name = "Status")]
        public required string Status { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
