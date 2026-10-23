using System.ComponentModel.DataAnnotations;

namespace SportLab.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Display(Name = "User")]
        public int UserId { get; set; }
        public User? User { get; set; }


        [Required(ErrorMessage = "Please write a comment")]
        [Display(Name = "Comment")]
        public string? Comment { get; set; }

        [Display(Name = "Rating")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }


        [Display(Name = "Date Created")]
        [DataType(DataType.Date)]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Moderation Status")]
        public required string ModerationStatus { get; set; }
    }
}
