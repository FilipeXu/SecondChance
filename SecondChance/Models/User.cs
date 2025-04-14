using Microsoft.AspNetCore.Identity;

namespace SecondChance.Models
{
    /// <summary>
    /// Representa um utilizador da plataforma.
    /// Estende a classe IdentityUser com informações adicionais específicas da aplicação.
    /// </summary>
    public class User : IdentityUser
    {
        /// <summary>
        /// Nome completo do utilizador
        /// </summary>
        public string FullName { get; set; }
        
        /// <summary>
        /// Data de nascimento do utilizador
        /// </summary>
        public DateTime BirthDate { get; set; }
        
        /// <summary>
        /// Data de registo do utilizador na plataforma
        /// </summary>
        public DateTime JoinDate { get; set; }
        
        /// <summary>
        /// Localização/morada do utilizador
        /// </summary>
        public string Location { get; set; }
        
        /// <summary>
        /// Caminho para a imagem de perfil do utilizador
        /// </summary>
        public string Image { get; set; }
        
        /// <summary>
        /// Descrição ou biografia do utilizador
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// Indica se o utilizador está ativo na plataforma
        /// </summary>
        public bool IsActive { get; set; }
        
        /// <summary>
        /// Indica se o utilizador foi permanentemente desativado
        /// </summary>
        public bool PermanentlyDisabled { get; set; }

        /// <summary>
        /// Indica se o utilizador tem privilégios de administrador
        /// </summary>
        public bool IsAdmin { get; set; }
        
        /// <summary>
        /// Indica se o utilizador foi o primeiro a registar-se na plataforma
        /// </summary>
        public bool IsFirstUser { get; set; }

        /// <summary>
        /// Comentários recebidos pelo utilizador
        /// </summary>
        public ICollection<Comment> ReceivedComments { get; set; }
        
        /// <summary>
        /// Comentários escritos pelo utilizador
        /// </summary>
        public ICollection<Comment> WrittenComments { get; set; }

        /// <summary>
        /// Mensagens enviadas pelo utilizador
        /// </summary>
        public ICollection<ChatMessage> SentMessages { get; set; }

        /// <summary>
        /// Mensagens recebidas pelo utilizador
        /// </summary>
        public ICollection<ChatMessage> ReceivedMessages { get; set; }
        
        /// <summary>
        /// Avaliações recebidas pelo utilizador
        /// </summary>
        public ICollection<UserRating> ReceivedRatings { get; set; }
        
        /// <summary>
        /// Avaliações atribuídas pelo utilizador a outros
        /// </summary>
        public ICollection<UserRating> GivenRatings { get; set; }

        /// <summary>
        /// Construtor que inicializa as coleções do utilizador
        /// </summary>
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
