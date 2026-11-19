using System.ComponentModel.DataAnnotations;

namespace SportLab.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Validation.UserRequired")]
        [Display(Name = "Field.User")]
        public int UserId { get; set; }
        public User? User { get; set; }


        [Required(ErrorMessage = "Validation.CommentRequired")]
        [StringLength(1000, MinimumLength = 3, ErrorMessage = "Validation.CommentLength")]
        [Display(Name = "Field.Comment")]
        public string? Comment { get; set; }

        [Required(ErrorMessage = "Validation.RatingRequired")]
        [Range(1, 5, ErrorMessage = "Validation.RatingRange")]
        [Display(Name = "Field.Rating")]
        public int Rating { get; set; }


        [Required]
        [Display(Name = "Field.DateCreated")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Validation.ModerationStatusRequired")]
        [Display(Name = "Field.ModerationStatus")]
        public required string ModerationStatus { get; set; }
    }
}
