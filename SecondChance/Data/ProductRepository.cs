using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SecondChance.Models;

namespace SecondChance.Data
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetUserProductsAsync(string userId, string sortOrder = null)
        {
            var productsQuery = _context.Products
                .Include(p => p.User)
                .Where(p => p.OwnerId == userId);               
            switch (sortOrder)
            {
                case "category":
                    productsQuery = productsQuery.OrderBy(p => p.Category);
                    break;
                case "distance":        
                    productsQuery = productsQuery.OrderBy(p => p.Location);
                    break;
                default:  
                    productsQuery = productsQuery.OrderByDescending(p => p.PublishDate);
                    break;
            }
            return await productsQuery.Take(3).ToListAsync();
        }
    }
}