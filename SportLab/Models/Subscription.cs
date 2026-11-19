using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    public class Subscription
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Field.User")]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        [Display(Name = "Field.Tariff")]
        public int PlanId { get; set; }

        [ForeignKey(nameof(PlanId))]
        public Plan? Plan { get; set; }

        [NotMapped]
        public int TariffId
        {
            get => PlanId;
            set => PlanId = value;
        }

        [NotMapped]
        public Plan? Tariff
        {
            get => Plan;
            set => Plan = value;
        }

        [Required]
        [Display(Name = "Field.StartDate")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Field.EndDate")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);

        [Display(Name = "Field.IsActive")]
        public bool IsActive { get; set; } = true;
    }
}
