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
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<User> _userManager;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<User> userManager)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string category, string location, string searchTerm, string sortOrder, string userId)
        {
            if (_context.Products == null)
                return Problem("Entity set 'ApplicationDbContext.Products' is null");

            var query = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm));

            if (!string.IsNullOrEmpty(category) && category != "Todas")
                query = query.Where(p => p.Category == category);

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
                case "name_asc":
                    query = query.OrderBy(p => p.Name);
                    break;
                case "name_desc":
                    query = query.OrderByDescending(p => p.Name);
                    break;
                default:
                    query = query.OrderByDescending(p => p.PublishDate);
                    break;
            }

            ViewBag.Categories = await _context.Products
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

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

            return View(await query.ToListAsync());
        }

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

        [Authorize]
        public IActionResult Create()
        {
            var product = new Product();

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = _context.Users.FirstOrDefault(u => u.Id == userId);
                    product.Location = user != null && !string.IsNullOrEmpty(user.Location) ?
                        user.Location : "Localização não especificada";
                }
            }

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Category,Image,PublishDate")] Product product,
            List<IFormFile> imageFiles, int mainImageIndex = 0)
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
                        product.ProductImages.Add(new ProductImage { ImagePath = imagePath });

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Category,Image,PublishDate,OwnerId")] Product product,
            List<IFormFile> imageFiles, int mainImageIndex = 0, string mainImagePath = "", List<int> removedImageIds = null)
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
                if (removedImageIds != null && removedImageIds.Count > 0)
                {
                    foreach (var imageId in removedImageIds)
                    {
                        var imageToRemove = existingProduct.ProductImages.FirstOrDefault(img => img.Id == imageId);
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

                    var remainingImages = existingProduct.ProductImages
                        .Where(img => !removedImageIds.Contains(img.Id)).ToList();

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
                            existingProduct.ProductImages.Add(new ProductImage { ImagePath = imagePath });

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

        public async Task<IActionResult> UserProducts(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return NotFound();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var products = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.User)
                .Where(p => p.OwnerId == userId)
                .OrderByDescending(p => p.PublishDate)
                .ToListAsync();

            ViewBag.FilteredUserName = user.FullName;
            ViewBag.UserId = userId;
            ViewBag.IsCurrentUser = User.Identity.IsAuthenticated &&
                User.FindFirstValue(ClaimTypes.NameIdentifier) == userId;

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

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
