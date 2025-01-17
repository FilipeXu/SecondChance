using Microsoft.AspNetCore.Identity;

namespace SecondChance.Models
{
    public class User: IdentityUser
    {
        public int Id {  get; set; }
        public string FullName { get; set; }
        public DateTime BirthDate { get; set; }
    }
}
