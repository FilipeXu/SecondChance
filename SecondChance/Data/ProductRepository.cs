using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SecondChance.Models;

namespace SecondChance.Data
{
    /// <summary>
    /// Implementação do repositório de produtos.
    /// Fornece acesso aos dados dos produtos na base de dados.
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Construtor do ProductRepository.
        /// </summary>
        /// <param name="context">Contexto da base de dados</param>
        public ProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtém a lista dos três produtos mais recentes de um utilizador específico.
        /// Permite ordenação por categoria, distância ou data de publicação.
        /// </summary>
        /// <param name="userId">ID do utilizador</param>
        /// <param name="sortOrder">Ordem de classificação: "category", "distance" ou null para data</param>
        /// <returns>Lista dos produtos mais recentes do utilizador</returns>
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
            return await productsQuery.ToListAsync();
        }
    }
}