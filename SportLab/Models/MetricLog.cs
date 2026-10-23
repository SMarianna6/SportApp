using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    public class MetricLog
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "User is required")]
        [Display(Name = "User")]
        public int UserId { get; set; }

        public User? User { get; set; }

        [Required(ErrorMessage = "Please enter current weight")]
        [Display(Name = "Weight (kg)")]
        [Range(20.0, 300.0, ErrorMessage = "Weight must be between 20 and 300 kg")]
        public float Weight { get; set; }

        [Required]
        [Display(Name = "Record Date")]
        [DataType(DataType.Date)]
        public DateTime RecordDate { get; set; } = DateTime.Now;

        [Display(Name = "Notes")]
        [StringLength(250)]
        public string? Notes { get; set; }
    }
}
