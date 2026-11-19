using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    public class MetricLog
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Validation.UserRequired")]
        [Display(Name = "Field.User")]
        public int UserId { get; set; }

        public User? User { get; set; }

        [Required(ErrorMessage = "Validation.WeightRequired")]
        [Display(Name = "Field.WeightKg")]
        [Range(20.0, 300.0, ErrorMessage = "Validation.WeightRange")]
        public float Weight { get; set; }

        [Required]
        [Display(Name = "Field.RecordDate")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime RecordDate { get; set; } = DateTime.Now;

        [Display(Name = "Field.Notes")]
        [StringLength(250)]
        public string? Notes { get; set; }
    }
}
