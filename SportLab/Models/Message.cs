using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportLab.Models
{
    public class Message
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Validation.SenderRequired")]
        [Display(Name = "Field.Sender")]
        public int SenderId { get; set; }

        [ForeignKey(nameof(SenderId))]
        public User? Sender { get; set; }

        [Required(ErrorMessage = "Validation.ReceiverRequired")]
        [Display(Name = "Field.Receiver")]
        public int ReceiverId { get; set; }

        [ForeignKey(nameof(ReceiverId))]
        public User? Receiver { get; set; }

        [Required(ErrorMessage = "Validation.MessageRequired")]
        [StringLength(2000, MinimumLength = 1, ErrorMessage = "Validation.MessageLength")]
        [Display(Name = "Field.Message")]
        public required string Text { get; set; }

        [Required]
        [Display(Name = "Field.DateSent")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = false)]
        public DateTime SentDate { get; set; } = DateTime.Now;

        [Display(Name = "Field.IsRead")]
        public bool IsRead { get; set; }
    }
}
