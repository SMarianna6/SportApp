using SportLab.Models;

namespace SportLab.Models
{
    public class ContactViewModel
    {
        public int Id { get; set; }  // User Id, or 0 for Admin
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int UnreadCount { get; set; }
        public bool HasConversation { get; set; }
        public string? LastMessageText { get; set; }  // Added per TZ
        public DateTime LastMessageDate { get; set; } = DateTime.MinValue;  // Added per TZ
    }
}