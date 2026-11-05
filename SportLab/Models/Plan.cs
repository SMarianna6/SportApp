using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    [Table("Tariffs")]
    public class Plan
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Plan name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        [Display(Name = "Plan Name")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [Display(Name = "Description")]
        public required string Description { get; set; }

        [Range(0.01, 100000.00, ErrorMessage = "Price must be between 0.01 and 100000")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Price")]
        [DataType(DataType.Currency)]
        [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = false)]
        public decimal Price { get; set; }

        [Range(1, 3650, ErrorMessage = "Duration must be between 1 and 3650 days")]
        [Display(Name = "Duration (days)")]
        public int Duration { get; set; }

        [Required(ErrorMessage = "Plan type is required")]
        [Display(Name = "Plan Type")]
        public string? Type { get; set; }

        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }
}