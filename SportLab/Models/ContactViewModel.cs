using SportLab.Models;

namespace SportLab.Models
{
    public class ContactViewModel
    {
        public string Id { get; set; } = string.Empty;  // User Id (as string) or "BROADCAST_TRAINERS" for admin broadcast
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int UnreadCount { get; set; }
        public bool HasConversation { get; set; }
        public string? LastMessageText { get; set; }  // Added per TZ
        public DateTime LastMessageDate { get; set; } = DateTime.MinValue;  // Added per TZ
    }
}