using System;
using System.ComponentModel.DataAnnotations;

namespace SecondChance.Models
{
    public class UserRating
    {
        public int Id { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
        public DateTime CreatedAt { get; set; }

        [Required]
        public string RatedUserId { get; set; }
        public User RatedUser { get; set; }

        [Required]
        public string RaterUserId { get; set; }
        public User RaterUser { get; set; }
    }
}