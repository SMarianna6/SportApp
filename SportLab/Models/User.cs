using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [Display(Name = "Full Name")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public required string Password { get; set; }

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
