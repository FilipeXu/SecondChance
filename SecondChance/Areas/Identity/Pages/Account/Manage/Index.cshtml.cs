// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;
using SecondChance.Models;
using SecondChance.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace SecondChance.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;

        public IndexModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ApplicationDbContext context,
            IProductRepository productRepository = null)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _productRepository = productRepository ?? new ProductRepository(context);
        }

        public User UserProfile { get; set; }
        public string Username { get; set; }
        public bool IsCurrentUser { get; set; }
        public string CurrentSort { get; set; }
        public List<Product> UserProducts { get; set; }
        public bool HasPassword { get; set; }

        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public int? CurrentUserRating { get; set; }
        public List<Comment> RecentComments { get; set; }
        public bool CanRate { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        [BindProperty]
        public IFormFile ImageFile { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "O nome é obrigatório")]
            [Display(Name = "Nome Completo")]
            public string FullName { get; set; }

            [Display(Name = "Imagem")]
            public string Image { get; set; }

            [Display(Name = "Localização")]
            public string Location { get; set; }

            [Display(Name = "Descrição")]
            public string Description { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Password atual")]
            public string OldPassword { get; set; }

            [StringLength(100, ErrorMessage = "A {0} deve ter pelo menos {2} e no máximo {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Nova password")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirmar nova password")]
            [Compare("NewPassword", ErrorMessage = "A nova password e a confirmação não coincidem.")]
            public string ConfirmPassword { get; set; }
        }



        private async Task LoadAsync(User user)
        {
            HasPassword = await _userManager.HasPasswordAsync(user);

            Input = new InputModel
            {
                FullName = user.FullName,
                Image = user.Image,
                Location = user.Location,
                Description = user.Description
            };

            UserProfile = user;
            IsCurrentUser = User.Identity.IsAuthenticated &&
                user.Id == _userManager.GetUserId(User);

            RecentComments = await _context.Comments
                .Include(c => c.Author)
                .Where(c => c.ProfileId == user.Id)
                .OrderByDescending(c => c.CreatedAt)
                .Take(3)
                .ToListAsync();

            await LoadRatingsAsync(user.Id);
        }

        private async Task LoadRatingsAsync(string userId)
        {

            AverageRating = await _context.UserRatings
                .Where(r => r.RatedUserId == userId)
                .AverageAsync(r => (double?)r.Rating) ?? 0;

            TotalRatings = await _context.UserRatings
                .Where(r => r.RatedUserId == userId)
                .CountAsync();

            CurrentUserRating = null;
            CanRate = false;

            if (User.Identity.IsAuthenticated)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null && currentUser.Id != userId)
                {
                    CanRate = true;
                    var rating = await _context.UserRatings
                        .FirstOrDefaultAsync(r => r.RaterUserId == currentUser.Id && r.RatedUserId == userId);
                    if (rating != null)
                    {
                        CurrentUserRating = rating.Rating;
                    }
                }
            }
        }

        public async Task<IActionResult> OnGetAsync(string userId = null, string sort = null)
        {
            User user;
            CurrentSort = sort;

            if (string.IsNullOrEmpty(userId))
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound($"Não foi possível carregar o usuário.");
                }
                IsCurrentUser = true;
            }
            else
            {
                user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound($"Não foi possível encontrar o usuário com ID '{userId}'.");
                }
                IsCurrentUser = User.Identity.IsAuthenticated &&
                    userId == _userManager.GetUserId(User);
            }

            await LoadAsync(user);
            var allUserProducts = await _productRepository.GetUserProductsAsync(user.Id, sort);


            UserProducts = allUserProducts.Where(p => !p.IsDonated).ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Não foi possível carregar o usuário.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = Path.GetFileName(ImageFile.FileName);
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + fileName;

                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");
                Directory.CreateDirectory(uploads);
                var filePath = Path.Combine(uploads, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(fileStream);
                }

                user.Image = "/Images/" + uniqueFileName;
            }

            user.Location = Input.Location;
            user.Description = Input.Description;
            user.FullName = Input.FullName;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                StatusMessage = "Erro inesperado ao tentar atualizar o perfil.";
                await LoadAsync(user);
                return Page();
            }

            bool hasPassword = await _userManager.HasPasswordAsync(user);
            if (hasPassword && !string.IsNullOrEmpty(Input.OldPassword) && !string.IsNullOrEmpty(Input.NewPassword))
            {
                var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
                if (!changePasswordResult.Succeeded)
                {
                    foreach (var error in changePasswordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    await LoadAsync(user);
                    return Page();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Seu perfil foi atualizado com sucesso";

            await LoadAsync(user);
            UserProducts = await _productRepository.GetUserProductsAsync(user.Id, CurrentSort);

            return Page();
        }
        public async Task<IActionResult> OnPostSubmitRatingAsync(string ratedUserId, int rating)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            if (rating < 1 || rating > 5)
            {
                StatusMessage = "Avaliação deve ser entre 1 e 5 estrelas.";
                return RedirectToPage(new { userId = ratedUserId });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound("Não foi possível carregar o usuário.");
            }

            if (currentUser.Id == ratedUserId)
            {
                StatusMessage = "Você não pode avaliar seu próprio perfil.";
                return RedirectToPage();
            }

            var existingRating = await _context.UserRatings
                .FirstOrDefaultAsync(r => r.RaterUserId == currentUser.Id && r.RatedUserId == ratedUserId);

            if (existingRating != null)
            {
                existingRating.Rating = rating;

            }
            else
            {

                _context.UserRatings.Add(new UserRating
                {
                    RaterUserId = currentUser.Id,
                    RatedUserId = ratedUserId,
                    Rating = rating,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            StatusMessage = "Avaliação enviada com sucesso!";
            return RedirectToPage(new { userId = ratedUserId });
        }

        public async Task<IActionResult> OnPostDeactivateAccountAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Não foi possível carregar o usuário.");
            }

            user.IsActive = false;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                StatusMessage = "Erro ao desativar a conta.";
                return RedirectToPage();
            }

            await _signInManager.SignOutAsync();
            return RedirectToPage("/Account/Login");
        }
    }
}