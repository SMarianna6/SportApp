using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    public class WorkoutSlot : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Activity name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Activity name must be between 2 and 100 characters")]
        [Display(Name = "Activity Name")]
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

        [Display(Name = "Capacity")]
        [Range(1, 1000, ErrorMessage = "Capacity must be between 1 and 1000")]
        public int Capacity { get; set; } = 15;

        [Display(Name = "Booked Spots")]
        [Range(0, 1000, ErrorMessage = "Booked spots cannot be negative")]
        public int Booked { get; set; } = 0;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndTime <= StartTime)
            {
                yield return new ValidationResult(
                    "End time must be later than the start time.",
                    new[] { nameof(EndTime) });
            }

            if (Booked > Capacity)
            {
                yield return new ValidationResult(
                    "Booked spots cannot exceed capacity.",
                    new[] { nameof(Booked) });
            }
        }
    }
}
