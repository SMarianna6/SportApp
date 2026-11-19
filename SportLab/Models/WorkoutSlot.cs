using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Extensions.Localization;

namespace SportLab.Models
{
    public class WorkoutSlot : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Validation.ActivityNameRequired")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Validation.ActivityNameLength")]
        [Display(Name = "Field.ActivityName")]
        public string? ActivityName { get; set; }

        [Required(ErrorMessage = "Validation.TrainerRequired")]
        [Display(Name = "Field.Trainer")]
        public int TrainerId { get; set; }

        [ForeignKey(nameof(TrainerId))]
        public User? Trainer { get; set; }

        [Display(Name = "Field.Client")]
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

        [Required(ErrorMessage = "Validation.StartTimeRequired")]
        [Display(Name = "Field.StartTime")]
        [DataType(DataType.DateTime)]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Validation.EndTimeRequired")]
        [Display(Name = "Field.EndTime")]
        [DataType(DataType.DateTime)]
        public DateTime EndTime { get; set; }

        [Required]
        [Display(Name = "Field.Status")]
        public string Status { get; set; } = "Available"; // "Available", "Booked", "Completed", "Cancelled"

        [Display(Name = "Field.Capacity")]
        [Range(1, 1000, ErrorMessage = "Validation.CapacityRange")]
        public int Capacity { get; set; } = 15;

        [Display(Name = "Field.BookedSpots")]
        [Range(0, 1000, ErrorMessage = "Validation.BookedSpotsRange")]
        public int Booked { get; set; } = 0;

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        [NotMapped]
        public int RemainingSpots => Math.Max(0, Capacity - Booked);

        [NotMapped]
        public bool IsFull => Booked >= Capacity;

        [NotMapped]
        public bool IsPast => EndTime <= DateTime.Now;

        [NotMapped]
        public bool IsBookable => !IsPast
                                  && !IsFull
                                  && Status == WorkoutStatuses.Available;

        public ICollection<WorkoutBooking> Bookings { get; set; } = new List<WorkoutBooking>();


        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var localizer = validationContext.GetService(typeof(IStringLocalizer<SharedResource>))
                as IStringLocalizer<SharedResource>;

            if (EndTime <= StartTime)
            {
                yield return new ValidationResult(
                    localizer?["Validation.EndTimeAfterStart"].Value
                        ?? "End time must be later than the start time.",
                    new[] { nameof(EndTime) });
            }

            if (Booked > Capacity)
            {
                yield return new ValidationResult(
                    localizer?["Validation.BookedExceedsCapacity"].Value
                        ?? "Booked spots cannot exceed capacity.",
                    new[] { nameof(Booked) });
            }
        }
    }

    public static class WorkoutStatuses
    {
        public const string Available = "Available";
        public const string Booked = "Booked";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
    }
}
