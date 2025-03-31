using Microsoft.AspNetCore.Identity;

namespace SecondChance.Models
{
    public class User : IdentityUser
    {
        public string FullName { get; set; }
        public DateTime BirthDate { get; set; }
        public DateTime JoinDate { get; set; }
        public string Location { get; set; }
        public string Image { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public bool PermanentlyDisabled { get; set; }

        public bool IsAdmin { get; set; }
        public bool IsFirstUser { get; set; }

        public ICollection<Comment> ReceivedComments { get; set; }
        public ICollection<Comment> WrittenComments { get; set; }

        public ICollection<ChatMessage> SentMessages { get; set; }

        public ICollection<ChatMessage> ReceivedMessages { get; set; }
        public ICollection<UserRating> ReceivedRatings { get; set; }
        public ICollection<UserRating> GivenRatings { get; set; }

        public User()
        {
            ReceivedComments = new List<Comment>();
            WrittenComments = new List<Comment>();
            SentMessages = new List<ChatMessage>();
            ReceivedMessages = new List<ChatMessage>();
            ReceivedRatings = new List<UserRating>();
            GivenRatings = new List<UserRating>();
        }
    }
}
