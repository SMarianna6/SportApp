using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Name must be between 2 and 100 characters")]
        [Display(Name = "Full Name")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
        [Display(Name = "Email Address")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "Password must be between 6 and 100 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public required string Password { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [StringLength(50, ErrorMessage = "Role cannot exceed 50 characters")]
        [Display(Name = "Role")]
        public required string Role { get; set; }

        [Display(Name = "Is Blocked")]
        public bool IsBlocked { get; set; }

        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        public ICollection<WorkoutSlot> WorkoutSlots { get; set; } = new List<WorkoutSlot>();
        public ICollection<MetricLog> MetricLogs { get; set; } = new List<MetricLog>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        [InverseProperty("Sender")]
        public ICollection<Message> SentMessages { get; set; } = new List<Message>();

        [InverseProperty("Receiver")]
        public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
    }
}