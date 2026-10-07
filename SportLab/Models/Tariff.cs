using System.ComponentModel.DataAnnotations;

namespace SportLab.Models
{
    public class Tariff
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        public int DurationDays { get; set; }

        public string Type { get; set; }
    }
}