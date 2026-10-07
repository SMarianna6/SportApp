using System;
using System.ComponentModel.DataAnnotations;

namespace SportLab.Models
{
    public class WorkoutSlot
    {
        public int Id { get; set; }

        public int TrainerId { get; set; }

        public int? ClientId { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public string Status { get; set; } 
    }
}