namespace SportLab.Models
{
    public class MetricLog
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public float Weight { get; set; }
        public DateTime RecordDate { get; set; } = DateTime.Now;
    }
}
