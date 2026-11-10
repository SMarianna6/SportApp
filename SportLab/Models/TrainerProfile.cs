using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    /// <summary>
    /// Extended profile information for a Trainer user.
    /// One-to-one with <see cref="User"/>.
    /// </summary>
    public class TrainerProfile
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(1000)]
        [Display(Name = "About")]
        public string? About { get; set; }

        [StringLength(255)]
        [Display(Name = "Experience")]
        public string? Experience { get; set; }

        /// <summary>
        /// Comma-separated list of specialization tags,
        /// e.g. "CrossFit,Strength,HIIT"
        /// </summary>
        [StringLength(500)]
        [Display(Name = "Specializations")]
        public string? Specializations { get; set; }

        [StringLength(500)]
        [Display(Name = "Photo URL")]
        public string? PhotoUrl { get; set; }
    }
}
