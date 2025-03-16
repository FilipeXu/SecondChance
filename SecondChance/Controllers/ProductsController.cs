using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SecondChance.Data;
using SecondChance.Models;

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


        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
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
                    ModelState.AddModelError("", "É necessário adicionar pelo menos uma imagem do produto.");
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
                    ModelState.AddModelError("", "É necessário adicionar pelo menos uma imagem válida do produto.");
                    return View(product);
                }

                _context.Add(product);
                await _context.SaveChangesAsync();
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                TempData["Message"] = "Produto criado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao criar produto: " + ex.Message);
                ModelState.AddModelError("", "Ocorreu um erro ao criar o produto: " + ex.Message);
            }

            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Category,Image,Location,PublishDate,OwnerId")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
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
