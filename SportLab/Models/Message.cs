using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    public class Message
    {
        public int Id { get; set; }

        [Display(Name = "Sender")]
        public int SenderId { get; set; }

        [ForeignKey(nameof(SenderId))]
        public User? Sender { get; set; }

        [Display(Name = "Receiver")]
        public int ReceiverId { get; set; }

        [ForeignKey(nameof(ReceiverId))]
        public User? Receiver { get; set; }

        [Required(ErrorMessage = "Message text cannot be empty")]
        [Display(Name = "Message")]
        public required string Text { get; set; }

        [Display(Name = "Date Sent")]
        [DataType(DataType.DateTime)]
        public DateTime SentDate { get; set; } = DateTime.Now;
    }
}
