using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondChance.Data;
using SecondChance.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace SecondChance.Controllers
{
    /// <summary>
    /// Controlador responsável pela gestão de produtos na plataforma.
    /// Implementa funcionalidades para listar, criar, editar, eliminar e gerir produtos.
    /// </summary>
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<User> _userManager;
        private const int PageSize = 9; 

        /// <summary>
        /// Construtor do ProductsController.
        /// </summary>
        /// <param name="context">Contexto da base de dados</param>
        /// <param name="webHostEnvironment">Ambiente de hospedagem para gestão de ficheiros</param>
        /// <param name="userManager">Gestor de utilizadores</param>
        public ProductsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<User> userManager)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
        }

        /// <summary>
        /// Apresenta a lista de produtos com opções de filtro e paginação.
        /// </summary>
        /// <param name="searchTerm">Termo de pesquisa para filtrar produtos</param>
        /// <param name="category">Categoria para filtrar produtos</param>
        /// <param name="location">Localização para filtrar produtos</param>
        /// <param name="sortOrder">Ordem de classificação dos produtos</param>
        /// <param name="userId">ID do utilizador para filtrar produtos</param>
        /// <param name="page">Número da página atual</param>
        /// <returns>Vista com lista de produtos filtrada e paginada</returns>
        public async Task<IActionResult> Index(string searchTerm, string category, string location, string sortOrder, string userId, int page = 1)
        {
            if (_context.Products == null)
                return Problem("Entity set 'ApplicationDbContext.Products' is null");

            var query = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.User)
                .Where(p => !p.IsDonated) 
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm));

            if (!string.IsNullOrEmpty(category) && category != "Todas")
            {
                if (Enum.TryParse<Category>(category, out Category categoryEnum))
                {
                    query = query.Where(p => p.Category == categoryEnum);
                }
            }

            if (!string.IsNullOrEmpty(location) && location != "Todas")
                query = query.Where(p => p.Location == location);

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(p => p.OwnerId == userId);
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                    ViewBag.FilteredUserName = user.FullName;
            }

            switch (sortOrder)
            {
                case "date_desc":
                    query = query.OrderByDescending(p => p.PublishDate);
                    break;
                case "date_asc":
                    query = query.OrderBy(p => p.PublishDate);
                    break;
                case "relevance":
                    query = query.OrderByDescending(p => _context.UserRatings
                        .Where(r => r.RatedUserId == p.OwnerId)
                        .Average(r => (double?)r.Rating) ?? 0);
                    break;
                default:
                    query = query.OrderByDescending(p => p.PublishDate);
                    break;
            }

            ViewBag.Categories = Enum.GetValues(typeof(Category))
                .Cast<Category>()
                .OrderBy(c => c.ToString())
                .ToList();

            ViewBag.Locations = await _context.Products
                .Select(p => p.Location)
                .Where(l => !string.IsNullOrEmpty(l))
                .Distinct()
                .OrderBy(l => l)
                .ToListAsync();

            ViewBag.CurrentSearchTerm = searchTerm;
            ViewBag.CurrentCategory = category;
            ViewBag.CurrentLocation = location;
            ViewBag.CurrentSortOrder = sortOrder;
            ViewBag.CurrentUserId = userId;
            ViewBag.CurrentPage = page;
            
            var totalProducts = await query.CountAsync();
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalPages = (int)Math.Ceiling(totalProducts / (double)PageSize);
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < ViewBag.TotalPages;
            
            var products = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return View(products);
        }
       
        

        /// <summary>
        /// Apresenta os detalhes de um produto específico.
        /// </summary>
        /// <param name="id">ID do produto</param>
        /// <returns>Vista com detalhes do produto</returns>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var product = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
                return NotFound();

            if (product.User == null && !string.IsNullOrEmpty(product.OwnerId))
                product.User = await _userManager.FindByIdAsync(product.OwnerId);

            if (product.User != null && !string.IsNullOrEmpty(product.User.Location) &&
                product.Location != product.User.Location)
            {
                product.Location = product.User.Location;
                await _context.SaveChangesAsync();
            }

            return View(product);
        }

        /// <summary>
        /// Apresenta o formulário para criar um novo produto.
        /// </summary>
        /// <returns>Vista com formulário de criação de produto</returns>
        [Authorize]
        public IActionResult Create()
        {
            var product = new Product
            {
                Name = "",
                Description = "",
                Location = "Por definir",
                OwnerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous"
            };

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = _context.Users.FirstOrDefault(u => u.Id == userId);
                    if (user != null && !string.IsNullOrEmpty(user.Location))
                    {
                        product.Location = user.Location;
                    }
                }
            }

            return View(product);
        }

        /// <summary>
        /// Processa a criação de um novo produto com imagens.
        /// </summary>
        /// <param name="product">Dados do produto</param>
        /// <param name="imageFiles">Lista de ficheiros de imagem</param>
        /// <param name="mainImageIndex">Índice da imagem principal</param>
        /// <returns>Redireciona para a lista de produtos após criação bem-sucedida</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Category,Image,PublishDate")] Product product,
            List<IFormFile>? imageFiles, int mainImageIndex = 0)
        {
            try
            {
                if (imageFiles == null || imageFiles.Count == 0 || imageFiles.All(f => f.Length == 0))
                {
                    ModelState.AddModelError("", "É necessário adicionar pelo menos uma imagem do produto");
                    return View(product);
                }

                product.PublishDate = DateTime.Now;

                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    product.OwnerId = !string.IsNullOrEmpty(userId) ? userId : "anonymous";

                    if (!string.IsNullOrEmpty(userId))
                    {
                        var user = await _userManager.FindByIdAsync(userId);
                        product.Location = user != null && !string.IsNullOrEmpty(user.Location) ?
                            user.Location : "Por definir";
                    }
                }
                else
                {
                    product.OwnerId = "anonymous";
                    product.Location = "Por definir";
                }

                product.ProductImages = new List<ProductImage>();

                if (mainImageIndex < 0 || mainImageIndex >= imageFiles.Count)
                    mainImageIndex = 0;

                for (int i = 0; i < imageFiles.Count; i++)
                {
                    var currentImage = imageFiles[i];
                    if (currentImage.Length > 0)
                    {
                        var fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(currentImage.FileName);
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Images");
                        var filePath = Path.Combine(uploadsFolder, fileName);

                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await currentImage.CopyToAsync(fileStream);
                        }

                        var imagePath = "/Images/" + fileName;
                        var productImage = new ProductImage { ImagePath = imagePath, Product = product };
                        product.ProductImages.Add(productImage);

                        if (i == mainImageIndex)
                            product.Image = imagePath;
                    }
                }

                if (product.ProductImages.Count == 0)
                {
                    ModelState.AddModelError("", "É necessário adicionar pelo menos uma imagem válida do produto");
                    return View(product);
                }

                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao criar produto: " + ex.Message);
                ModelState.AddModelError("", "Ocorreu um erro ao criar o produto: " + ex.Message);
            }

            return View(product);
        }

        /// <summary>
        /// Apresenta o formulário para editar um produto existente.
        /// </summary>
        /// <param name="id">ID do produto a ser editado</param>
        /// <returns>Vista com formulário de edição do produto</returns>
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var product = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (product.OwnerId != userId)
            {
                TempData["Message"] = "Não tem permissão para editar este produto";
                return RedirectToAction(nameof(Index));
            }

            if (product.User != null && !string.IsNullOrEmpty(product?.User?.Location))
                product.Location = product.User.Location;

            return View(product);
        }

        /// <summary>
        /// Processa a edição de um produto existente.
        /// </summary>
        /// <param name="id">ID do produto</param>
        /// <param name="product">Dados atualizados do produto</param>
        /// <param name="imageFiles">Novas imagens a serem adicionadas</param>
        /// <param name="mainImageIndex">Índice da nova imagem principal</param>
        /// <param name="mainImagePath">Caminho da imagem principal existente</param>
        /// <param name="removedImageIds">IDs das imagens a serem removidas</param>
        /// <returns>Redireciona para a lista de produtos após edição bem-sucedida</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Category,Image,PublishDate,OwnerId")] Product product,
            List<IFormFile>? imageFiles = null, int mainImageIndex = 0, string? mainImagePath = null, List<int>? removedImageIds = null)
        {
            if (id != product.Id)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (product.OwnerId != userId)
            {
                TempData["Message"] = "Não tem permissão para editar este produto";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var existingProduct = await _context.Products
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (existingProduct == null)
                    return NotFound();

                if (existingProduct.OwnerId != userId)
                {
                    TempData["Message"] = "Não tem permissão para editar este produto";
                    return RedirectToAction(nameof(Index));
                }

                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Category = product.Category;

                if (User.Identity?.IsAuthenticated == true)
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user != null && !string.IsNullOrEmpty(user?.Location))
                        existingProduct.Location = user.Location;
                }
                if (removedImageIds?.Any() == true && existingProduct.ProductImages?.Any() == true)
                {
                    foreach (var imageId in removedImageIds)
                    {
                        var imageToRemove = existingProduct.ProductImages?.FirstOrDefault(img => img.Id == imageId);
                        if (imageToRemove != null)
                        {
                            var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageToRemove.ImagePath.TrimStart('/'));
                            if (System.IO.File.Exists(imagePath))
                            {
                                try
                                {
                                    System.IO.File.Delete(imagePath);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Erro ao excluir arquivo de imagem: {ex.Message}");
                                }
                            }

                            _context.Entry(imageToRemove).State = EntityState.Deleted;
                        }
                    }

                    var remainingImages = existingProduct.ProductImages?
                        .Where(img => !removedImageIds.Contains(img.Id))
                        .ToList() ?? new List<ProductImage>();

                    if (remainingImages.Any() &&
                        !remainingImages.Any(img => img.ImagePath == existingProduct.Image))
                    {
                        existingProduct.Image = remainingImages.First().ImagePath;
                    }
                }
                if (imageFiles != null && imageFiles.Count > 0)
                {
                    if (mainImageIndex < 0 || mainImageIndex >= imageFiles.Count)
                        mainImageIndex = 0;

                    if (existingProduct.ProductImages == null)
                        existingProduct.ProductImages = new List<ProductImage>();

                    for (int i = 0; i < imageFiles.Count; i++)
                    {
                        var currentImage = imageFiles[i];
                        if (currentImage.Length > 0)
                        {
                            var fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(currentImage.FileName);
                            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Images");
                            var filePath = Path.Combine(uploadsFolder, fileName);

                            if (!Directory.Exists(uploadsFolder))
                                Directory.CreateDirectory(uploadsFolder);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await currentImage.CopyToAsync(fileStream);
                            }

                            var imagePath = "/Images/" + fileName;
                            var productImage = new ProductImage { ImagePath = imagePath, Product = existingProduct };
                            existingProduct.ProductImages.Add(productImage);

                            if (i == mainImageIndex)
                                existingProduct.Image = imagePath;
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(mainImagePath))
                {
                    bool isValidImagePath = existingProduct.ProductImages?
                        .Any(image => image.ImagePath == mainImagePath) ?? false;

                    if (isValidImagePath)
                        existingProduct.Image = mainImagePath;
                }

                bool hasRemainingImages = existingProduct.ProductImages != null &&
                    existingProduct.ProductImages.Any(img => removedImageIds == null || !removedImageIds.Contains(img.Id));

                if (!hasRemainingImages && (imageFiles == null || imageFiles.Count == 0))
                {
                    ModelState.AddModelError("", "O produto precisa de ter pelo menos uma imagem.");
                    return View(product);
                }

                _context.Update(existingProduct);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Produto atualizado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Products.Any(e => e.Id == product.Id))
                    return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error editing product: " + ex.Message);
                ModelState.AddModelError("", "An error occurred while editing the product: " + ex.Message);
            }

            return View(product);
        }

        /// <summary>
        /// Apresenta a página de confirmação para eliminar um produto.
        /// </summary>
        /// <param name="id">ID do produto a ser eliminado</param>
        /// <returns>Vista de confirmação de eliminação</returns>
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var product = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (product.OwnerId != userId)
            {
                TempData["Message"] = "Não tem permissão para excluir este produto.";
                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        /// <summary>
        /// Processa a eliminação de um produto.
        /// </summary>
        /// <param name="id">ID do produto a ser eliminado</param>
        /// <returns>Redireciona para a lista de produtos após eliminação bem-sucedida</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (product.OwnerId != userId)
            {
                TempData["Message"] = "Não tem permissão para excluir este produto.";
                return RedirectToAction(nameof(Index));
            }
            if (product.ProductImages != null)
            {
                foreach (var image in product.ProductImages)
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, image.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        try { System.IO.File.Delete(imagePath); }
                        catch (Exception ex) { Console.WriteLine($"Error deleting image file: {ex.Message}"); }
                    }
                }
            }
            if (!string.IsNullOrEmpty(product.Image))
            {
                var mainImagePath = Path.Combine(_webHostEnvironment.WebRootPath, product.Image.TrimStart('/'));
                if (System.IO.File.Exists(mainImagePath))
                {
                    try { System.IO.File.Delete(mainImagePath); }
                    catch (Exception ex) { Console.WriteLine($"Error deleting main image file: {ex.Message}"); }
                }
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Produto excluído com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Lista os produtos de um utilizador específico.
        /// </summary>
        /// <param name="userId">ID do utilizador</param>
        /// <param name="page">Número da página atual</param>
        /// <returns>Vista com lista de produtos do utilizador</returns>
        public async Task<IActionResult> UserProducts(string userId, int page = 1)
        {
            if (string.IsNullOrEmpty(userId))
                return NotFound();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var query = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.User)
                .Where(p => p.OwnerId == userId)
                .Where(p => !p.IsDonated)
                .OrderByDescending(p => p.PublishDate);

            ViewBag.FilteredUserName = user.FullName;
            ViewBag.UserId = userId;
            ViewBag.CurrentUserId = userId;
            ViewBag.CurrentPage = page;
            ViewBag.IsCurrentUser = User.Identity?.IsAuthenticated == true &&
                User.FindFirstValue(ClaimTypes.NameIdentifier) == userId;

            var totalProducts = await query.CountAsync();
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalPages = (int)Math.Ceiling(totalProducts / (double)PageSize);
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < ViewBag.TotalPages;

            var products = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewBag.Categories = await _context.Products
                .Where(p => p.OwnerId == userId)
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Locations = await _context.Products
                .Where(p => p.OwnerId == userId)
                .Select(p => p.Location)
                .Distinct()
                .OrderBy(l => l)
                .ToListAsync();

            return View("Index", products);
        }

        /// <summary>
        /// Verifica se um produto existe na base de dados.
        /// </summary>
        /// <param name="id">ID do produto</param>
        /// <returns>Verdadeiro se o produto existir, falso caso contrário</returns>
        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        /// <summary>
        /// Marca um produto como doado.
        /// </summary>
        /// <param name="id">ID do produto</param>
        /// <returns>Redireciona para a lista de produtos após atualização bem-sucedida</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> MarkAsDonated(int id)
        {
            var product = await _context.Products.FindAsync(id);
            
            if (product == null)
                return NotFound();
                
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (product.OwnerId != userId)
            {
                TempData["Message"] = "Não tem permissão para marcar este produto como doado.";
                return RedirectToAction(nameof(Details), new { id = id });
            }
            
            product.IsDonated = true;
            product.DonatedDate = DateTime.Now;
            
            await _context.SaveChangesAsync();
            
            TempData["Message"] = "Produto marcado como doado com sucesso! Obrigado pela sua contribuição!";
            return RedirectToAction(nameof(Index));
        }
    }
}
