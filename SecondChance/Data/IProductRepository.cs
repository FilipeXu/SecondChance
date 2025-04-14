using System.Collections.Generic;
using System.Threading.Tasks;
using SecondChance.Models;

namespace SecondChance.Data
{
    /// <summary>
    /// Interface que define as operações disponíveis para acesso aos dados dos produtos.
    /// Implementa o padrão Repository para abstração do acesso à base de dados.
    /// </summary>
    public interface IProductRepository
    {
        /// <summary>
        /// Obtém a lista de produtos de um utilizador específico.
        /// </summary>
        /// <param name="userId">ID do utilizador</param>
        /// <param name="sortOrder">Ordem de classificação dos produtos</param>
        /// <returns>Lista de produtos do utilizador</returns>
        Task<List<Product>> GetUserProductsAsync(string userId, string sortOrder = null);
    }
}