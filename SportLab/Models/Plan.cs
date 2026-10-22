using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    public class Plan
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Назва тарифу є обов'язковою")]
        [StringLength(100, ErrorMessage = "Назва не може перевищувати 100 символів")]
        [Display(Name = "Назва тарифу")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Опис тарифу є обов'язковим")]
        [Display(Name = "Опис")]
        public required string Description { get; set; }

        [Required(ErrorMessage = "Ціна є обов'язковою")]
        [Range(0.01, 100000.00, ErrorMessage = "Ціна повинна бути більшою за 0")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Ціна (грн)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Тривалість є обов'язковою")]
        [Range(1, 3650, ErrorMessage = "Тривалість має становити від 1 до 3650 днів")]
        [Display(Name = "Тривалість (днів)")]
        public int Duration { get; set; }

        [Display(Name = "Тип тарифу")]
        public string? Type { get; set; }

        // Зв'язок 1:N (Один план/тариф містить багато підписок)
        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }
}
