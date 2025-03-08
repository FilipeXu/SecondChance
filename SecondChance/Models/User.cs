using Microsoft.AspNetCore.Identity;

namespace SecondChance.Models
{
    public class User: IdentityUser
    {
        public string FullName { get; set; }
        public DateTime BirthDate { get; set; }
        public DateTime JoinDate { get; set; }
        public string Location { get; set; }
        public string Image { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
}
