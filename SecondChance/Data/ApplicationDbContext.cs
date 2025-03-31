using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SecondChance.Models;

namespace SecondChance.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<SecondChance.Models.Product> Products { get; set; } = default!;
        public DbSet<Comment> Comments { get; set; } = default!;

        public DbSet<UserRating> UserRatings { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Author)
                .WithMany(u => u.WrittenComments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Profile)
                .WithMany(u => u.ReceivedComments)
                .HasForeignKey(c => c.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.ProductImages)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRating>()
                .HasOne(r => r.RatedUser)
                .WithMany(u => u.ReceivedRatings)
                .HasForeignKey(r => r.RatedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserRating>()
                .HasOne(r => r.RaterUser)
                .WithMany(u => u.GivenRatings)
                .HasForeignKey(r => r.RaterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserRating>()
                .HasIndex(r => new { r.RaterUserId, r.RatedUserId })
                .IsUnique();
        }
    }
}
