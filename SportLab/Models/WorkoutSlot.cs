namespace SportLab.Models
{
    public class WorkoutSlot
    {
        public int Id { get; set; }

        public int TrainerId { get; set; }
        public User? Trainer { get; set; }

        public int? ClientId { get; set; }
        public User? Client { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public required string Status { get; set; }
    }
}
