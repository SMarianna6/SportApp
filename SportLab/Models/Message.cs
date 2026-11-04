using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    public class Message
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Sender is required")]
        [Display(Name = "Sender")]
        public int SenderId { get; set; }

        [ForeignKey(nameof(SenderId))]
        public User? Sender { get; set; }

        [Required(ErrorMessage = "Receiver is required")]
        [Display(Name = "Receiver")]
        public int ReceiverId { get; set; }

        [ForeignKey(nameof(ReceiverId))]
        public User? Receiver { get; set; }

        [Required(ErrorMessage = "Message text cannot be empty")]
        [StringLength(2000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 2000 characters")]
        [Display(Name = "Message")]
        public required string Text { get; set; }

        [Required]
        [Display(Name = "Date Sent")]
        [DataType(DataType.DateTime)]
        public DateTime SentDate { get; set; } = DateTime.Now;
    }
}
