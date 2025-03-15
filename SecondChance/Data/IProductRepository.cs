using System.Collections.Generic;
using System.Threading.Tasks;
using SecondChance.Models;

namespace SecondChance.Data
{
    public interface IProductRepository
    {
        Task<List<Product>> GetUserProductsAsync(string userId, string sortOrder = null);
    }
}