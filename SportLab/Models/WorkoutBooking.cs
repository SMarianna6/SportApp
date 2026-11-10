using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    public class WorkoutBooking
    {
        public int Id { get; set; }

        [Required]
        public int WorkoutSlotId { get; set; }

        [ForeignKey(nameof(WorkoutSlotId))]
        public WorkoutSlot? WorkoutSlot { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        [Display(Name = "Booked At")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = false)]
        public DateTime BookingDate { get; set; } = DateTime.Now;

        /// <summary>Booked | Attended | NoShow — used by Trainer "My Clients" history.</summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = BookingStatuses.Booked;

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }

    public static class BookingStatuses
    {
        public const string Booked = "Booked";
        public const string Attended = "Attended";
        public const string NoShow = "NoShow";

        public static readonly string[] All = { Booked, Attended, NoShow };
    }
}