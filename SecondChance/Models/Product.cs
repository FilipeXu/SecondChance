namespace SecondChance.Models
{
    /// <summary>
    /// Representa um produto disponibilizado para doação na plataforma.
    /// Contém todas as informações necessárias para descrever um produto.
    /// </summary>
    public class Product
    {
        /// <summary>
        /// Identificador único do produto
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Nome do produto
        /// </summary>
        public required string Name { get; set; }
        
        /// <summary>
        /// Descrição detalhada do produto
        /// </summary>
        public required string Description { get; set; }
        
        /// <summary>
        /// Categoria à qual o produto pertence
        /// </summary>
        public Category Category { get; set; }
        
        /// <summary>
        /// Caminho da imagem principal do produto
        /// </summary>
        public string? Image { get; set; }
        
        /// <summary>
        /// Localização onde o produto está disponível
        /// </summary>
        public required string Location { get; set; }
        
        /// <summary>
        /// Data em que o produto foi publicado
        /// </summary>
        public DateTime PublishDate { get; set; }
        
        /// <summary>
        /// Identificador do utilizador proprietário do produto
        /// </summary>
        public required string OwnerId { get; set; }
        
        /// <summary>
        /// Utilizador proprietário do produto
        /// </summary>
        public User? User { get; set; }
        
        /// <summary>
        /// Indica se o produto já foi doado
        /// </summary>
        public bool IsDonated { get; set; }
        
        /// <summary>
        /// Data em que o produto foi doado
        /// </summary>
        public DateTime? DonatedDate { get; set; }
        
        /// <summary>
        /// Coleção de imagens associadas ao produto
        /// </summary>
        public ICollection<ProductImage>? ProductImages { get; set; }
    }


}
