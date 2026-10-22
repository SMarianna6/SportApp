using System.ComponentModel.DataAnnotations;

namespace SportLab.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ім'я є обов'язковим")]
        [Display(Name = "Ім'я")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Email є обов'язковим")]
        [EmailAddress(ErrorMessage = "Некоректний формат email")]
        [Display(Name = "Електронна пошта")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Пароль є обов'язковим")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public required string Password { get; set; }

        [Display(Name = "Роль")]
        public required string Role { get; set; }

        [Display(Name = "Заблоковано")]
        public bool IsBlocked { get; set; }

        // Зв'язок 1:N з Subscription (Користувач має багато підписок)
        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

        // Зв'язок 1:N з WorkoutSlot (Користувач може мати багато тренувальних слотів)
        public ICollection<WorkoutSlot> WorkoutSlots { get; set; } = new List<WorkoutSlot>();

        // Зв'язок 1:N з MetricLog (Користувач веде журнал своїх показників)
        public ICollection<MetricLog> MetricLogs { get; set; } = new List<MetricLog>();
    }
}
