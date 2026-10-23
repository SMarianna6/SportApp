using System.ComponentModel.DataAnnotations;

namespace SportLab.Models
{
    public class FAQ
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Question is required")]
        [Display(Name = "Question")]
        public required string Question { get; set; }

        [Required(ErrorMessage = "Answer is required")]
        [Display(Name = "Answer")]
        public required string Answer { get; set; }

        [Display(Name = "Sort Order")]
        public int SortOrder { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }
    }
}
