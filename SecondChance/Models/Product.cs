namespace SecondChance.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Image { get; set; }
        public string Location { get; set; }
        public DateTime PublishDate { get; set; }
        public string OwnerId { get; set; }
        public User? User { get; set; }
        public bool IsDonated { get; set; }
        public DateTime? DonatedDate { get; set; }

        public ICollection<ProductImage>? ProductImages { get; set; }
    }

    public class ProductImage
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ImagePath { get; set; }
        public Product Product { get; set; }
    }
}
