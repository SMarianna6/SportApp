using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    public class WorkoutSlot
    {
        public int Id { get; set; }

        [Display(Name = "Activity Name")]
        [StringLength(100)]
        public string? ActivityName { get; set; }

        [Required(ErrorMessage = "Trainer must be specified")]
        [Display(Name = "Trainer")]
        public int TrainerId { get; set; }

        [ForeignKey(nameof(TrainerId))]
        public User? Trainer { get; set; }

        [Display(Name = "Client")]
        public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

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

        [Required(ErrorMessage = "Start time is required")]
        [Display(Name = "Start Time")]
        [DataType(DataType.DateTime)]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "End time is required")]
        [Display(Name = "End Time")]
        [DataType(DataType.DateTime)]
        public DateTime EndTime { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Available"; // "Available", "Booked", "Completed", "Cancelled"
    }
}
