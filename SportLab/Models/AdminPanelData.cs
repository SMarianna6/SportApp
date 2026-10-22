namespace SportLab.Models
{
    public class AdminPanelData
    {
        public IEnumerable<Plan> Tariffs { get; set; } = new List<Plan>();
        public IEnumerable<Product> Products { get; set; } = new List<Product>();
        public IEnumerable<User> Users { get; set; } = new List<User>();
    }
}
