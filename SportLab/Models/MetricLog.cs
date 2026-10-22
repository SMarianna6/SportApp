using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    public class MetricLog
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Користувач")]
        public int UserId { get; set; }

        // Зв'язок 1:N з User (Журнал показників користувача)
        public User? User { get; set; }

        [Required(ErrorMessage = "Вкажіть поточну вагу")]
        [Display(Name = "Вага (кг)")]
        [Range(20.0, 300.0, ErrorMessage = "Вага повинна бути в діапазоні від 20 до 300 кг")]
        public float Weight { get; set; }

        [Required]
        [Display(Name = "Дата запису")]
        [DataType(DataType.Date)]
        public DateTime RecordDate { get; set; } = DateTime.Now;

        [Display(Name = "Примітки")]
        [StringLength(250)]
        public string? Notes { get; set; }
    }
}
