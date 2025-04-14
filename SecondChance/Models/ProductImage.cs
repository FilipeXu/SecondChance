namespace SecondChance.Models
{
    /// <summary>
    /// Representa uma imagem associada a um produto.
    /// </summary>
    public class ProductImage
    {
        /// <summary>
        /// Identificador único da imagem
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Identificador do produto ao qual a imagem está associada
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Caminho para acesso à imagem
        /// </summary>
        public required string ImagePath { get; set; }

        /// <summary>
        /// Produto ao qual a imagem pertence
        /// </summary>
        public required Product Product { get; set; }
    }
}
