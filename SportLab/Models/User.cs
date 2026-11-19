using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Validation.NameRequired")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Validation.NameLength")]
        [Display(Name = "Field.FullName")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Validation.EmailRequired")]
        [EmailAddress(ErrorMessage = "Validation.EmailFormat")]
        [StringLength(150, ErrorMessage = "Validation.EmailLength")]
        [Display(Name = "Field.Email")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Validation.PasswordRequired")]
        [StringLength(256, MinimumLength = 6,
            ErrorMessage = "Validation.PasswordLength")]
        [DataType(DataType.Password)]
        [Display(Name = "Field.Password")]
        public required string Password { get; set; }

        [Required(ErrorMessage = "Validation.RoleRequired")]
        [StringLength(50, ErrorMessage = "Validation.RoleLength")]
        [Display(Name = "Field.Role")]
        public required string Role { get; set; }

        [Display(Name = "Field.IsBlocked")]
        public bool IsBlocked { get; set; }

        [Display(Name = "Field.Avatar")]
        [StringLength(500)]
        public string? AvatarUrl { get; set; }

        [StringLength(120)]
        [Display(Name = "Field.Goal")]
        public string? Goal { get; set; }

        public TrainerProfile? TrainerProfile { get; set; }

        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        public ICollection<WorkoutSlot> WorkoutSlots { get; set; } = new List<WorkoutSlot>();
        public ICollection<MetricLog> MetricLogs { get; set; } = new List<MetricLog>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<WorkoutBooking> WorkoutBookings { get; set; } = new List<WorkoutBooking>();

        [InverseProperty("Sender")]
        public ICollection<Message> SentMessages { get; set; } = new List<Message>();

        [InverseProperty("Receiver")]
        public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
    }
}