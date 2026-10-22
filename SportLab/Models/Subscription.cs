using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    public class Subscription
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Користувач")]
        public int UserId { get; set; }

        // Зв'язок 1:N (Підписка належить одному користувачу)
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        [Display(Name = "Тарифний план")]
        public int PlanId { get; set; }

        // Зв'язок 1:N (Підписка прив'язана до одного тарифного плану)
        [ForeignKey(nameof(PlanId))]
        public Plan? Plan { get; set; }

        // Псевдоніми для зворотної сумісності з існуючими контролерами/поданнями
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
        [Display(Name = "Дата початку")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Дата завершення")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);

        [Display(Name = "Активна")]
        public bool IsActive { get; set; } = true;
    }
}
