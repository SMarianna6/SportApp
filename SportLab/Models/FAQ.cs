using System.ComponentModel.DataAnnotations;

namespace SportLab.Models
{
    public class FAQ
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Validation.QuestionRequired")]
        [Display(Name = "Field.Question")]
        public required string Question { get; set; }

        [Required(ErrorMessage = "Validation.AnswerRequired")]
        [Display(Name = "Field.Answer")]
        public required string Answer { get; set; }

        [Display(Name = "Field.SortOrder")]
        public int SortOrder { get; set; }

        [Display(Name = "Field.IsActive")]
        public bool IsActive { get; set; }
    }
}
