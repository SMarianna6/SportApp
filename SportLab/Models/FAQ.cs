namespace SportLab.Models
{
    public class FAQ
    {
        public int Id { get; set; }
        public required string Question { get; set; }
        public required string Answer { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
