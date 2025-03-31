namespace SecondChance.Models
{
    public enum Category
    {
        Roupa,
        Eletrônicos,
        Móveis,
        Livros,
        Decoração,
        Desporto,
        Brinquedos,
        Jardim,
        Outros
    }

    public class Product
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public Category Category { get; set; }
        public string? Image { get; set; }
        public required string Location { get; set; }
        public DateTime PublishDate { get; set; }
        public required string OwnerId { get; set; }
        public User? User { get; set; }
        public bool IsDonated { get; set; }
        public DateTime? DonatedDate { get; set; }
        public ICollection<ProductImage>? ProductImages { get; set; }
    }

    public class ProductImage
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public required string ImagePath { get; set; }
        public required Product Product { get; set; }
    }
}
