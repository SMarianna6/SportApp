using System.ComponentModel.DataAnnotations;

namespace SportLab.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Display(Name = "User")]
        public int UserId { get; set; }
        public User? User { get; set; }


        [Display(Name = "Order Date")]
        [DataType(DataType.DateTime)]
        public DateTime OrderDate { get; set; }

        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [Display(Name = "Status")]
        public required string Status { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
