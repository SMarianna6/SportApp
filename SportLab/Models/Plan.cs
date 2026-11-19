using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    [Table("Tariffs")]
    public class Plan
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Validation.PlanNameRequired")]
        [StringLength(100, ErrorMessage = "Validation.PlanNameLength")]
        [Display(Name = "Field.PlanName")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Validation.DescriptionRequired")]
        [Display(Name = "Field.Description")]
        public required string Description { get; set; }

        [Range(0.01, 100000.00, ErrorMessage = "Validation.PriceRange")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Field.Price")]
        [DataType(DataType.Currency)]
        [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = false)]
        public decimal Price { get; set; }

        [Range(1, 3650, ErrorMessage = "Validation.DurationRange")]
        [Display(Name = "Field.DurationDays")]
        public int Duration { get; set; }

        [Required(ErrorMessage = "Validation.PlanTypeRequired")]
        [Display(Name = "Field.PlanType")]
        public string? Type { get; set; }

        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }
}