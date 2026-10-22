using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    public class WorkoutSlot
    {
        public int Id { get; set; }

        [Display(Name = "Назва заняття")]
        [StringLength(100)]
        public string? ActivityName { get; set; }

        [Required(ErrorMessage = "Необхідно вказати тренера")]
        [Display(Name = "Тренер")]
        public int TrainerId { get; set; }

        [ForeignKey(nameof(TrainerId))]
        public User? Trainer { get; set; }

        [Display(Name = "Клієнт / Користувач")]
        public int? UserId { get; set; }

        // Зв'язок 1:N з User (Користувач може записатися на кілька тренувальних слотів)
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        // Псевдоніми для зворотної сумісності з попереднім ClientId / Client
        [NotMapped]
        public int? ClientId
        {
            get => UserId;
            set => UserId = value;
        }

        [NotMapped]
        public User? Client
        {
            get => User;
            set => User = value;
        }

        [Required(ErrorMessage = "Вкажіть час початку")]
        [Display(Name = "Час початку")]
        [DataType(DataType.DateTime)]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Вкажіть час закінчення")]
        [Display(Name = "Час завершення")]
        [DataType(DataType.DateTime)]
        public DateTime EndTime { get; set; }

        [Required]
        [Display(Name = "Статус")]
        public string Status { get; set; } = "Available"; // "Available", "Booked", "Completed", "Cancelled"
    }
}
