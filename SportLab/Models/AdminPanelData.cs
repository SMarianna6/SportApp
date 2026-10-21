namespace SportLab.Models
{
    public class AdminPanelData
    {
        public IEnumerable<Tariff> Tariffs { get; set; }
        public IEnumerable<Product> Products { get; set; }
        public IEnumerable<User> Users { get; set; }
    }
}
