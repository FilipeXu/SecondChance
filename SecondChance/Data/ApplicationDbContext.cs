using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SecondChance.Models;

namespace SecondChance.Data
{
    /// <summary>
    /// Contexto da base de dados da aplicação.
    /// Gere o acesso aos dados e define as relações entre as entidades.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        /// <summary>
        /// Construtor do ApplicationDbContext.
        /// </summary>
        /// <param name="options">Opções de configuração do contexto</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Coleção de produtos na base de dados
        /// </summary>
        public DbSet<Product> Products { get; set; } = default!;

        /// <summary>
        /// Coleção de comentários na base de dados
        /// </summary>
        public DbSet<Comment> Comments { get; set; } = default!;

        /// <summary>
        /// Coleção de mensagens de chat na base de dados
        /// </summary>
        public DbSet<ChatMessage> ChatMessages { get; set; } = default!;

        /// <summary>
        /// Coleção de denúncias de utilizadores na base de dados
        /// </summary>
        public DbSet<UserReport> UserReports { get; set; } = default!;

        /// <summary>
        /// Coleção de avaliações de utilizadores na base de dados
        /// </summary>
        public DbSet<UserRating> UserRatings { get; set; } = default!;

        /// <summary>
        /// Configura as relações entre as entidades e restrições da base de dados.
        /// </summary>
        /// <param name="modelBuilder">Construtor de modelo do Entity Framework</param>
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
            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasIndex(m => m.ConversationId);
            modelBuilder.Entity<UserReport>()
                .HasOne(r => r.ReportedUser)
                .WithMany()
                .HasForeignKey(r => r.ReportedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserReport>()
                .HasOne(r => r.ReporterUser)
                .WithMany()
                .HasForeignKey(r => r.ReporterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserReport>()
                .HasOne(r => r.ResolvedBy)
                .WithMany()
                .HasForeignKey(r => r.ResolvedById)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
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