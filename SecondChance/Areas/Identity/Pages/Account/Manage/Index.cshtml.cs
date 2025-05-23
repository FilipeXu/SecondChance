﻿// Licensed to the .NET Foundation under one or more agreements.
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
    /// <summary>
    /// Modelo da página de gestão de perfil de utilizador.
    /// Permite visualizar e editar informações do perfil, configurações da conta e preferências do utilizador.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Construtor do IndexModel.
        /// </summary>
        /// <param name="userManager">Gestor de utilizadores para acesso a funcionalidades de identidade</param>
        /// <param name="signInManager">Gestor de sessões para funcionalidades de autenticação</param>
        /// <param name="context">Contexto da base de dados</param>
        /// <param name="productRepository">Repositório de produtos, opcional</param>
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

        /// <summary>
        /// Perfil do utilizador a ser exibido ou editado
        /// </summary>
        public User UserProfile { get; set; }
        
        /// <summary>
        /// Nome de utilizador para exibição
        /// </summary>
        public string Username { get; set; }
        
        /// <summary>
        /// Indica se o perfil sendo visualizado pertence ao utilizador autenticado
        /// </summary>
        public bool IsCurrentUser { get; set; }
        
        /// <summary>
        /// Ordenação atual aplicada aos produtos do utilizador
        /// </summary>
        public string CurrentSort { get; set; }
        
        /// <summary>
        /// Lista de produtos pertencentes ao utilizador
        /// </summary>
        public List<Product> UserProducts { get; set; }
        
        /// <summary>
        /// Indica se o utilizador possui uma password configurada
        /// </summary>
        public bool HasPassword { get; set; }

        /// <summary>
        /// Avaliação média do utilizador
        /// </summary>
        public double AverageRating { get; set; }
        
        /// <summary>
        /// Total de avaliações recebidas pelo utilizador
        /// </summary>
        public int TotalRatings { get; set; }
        
        /// <summary>
        /// Avaliação atual do utilizador autenticado para o perfil sendo visualizado
        /// </summary>
        public int? CurrentUserRating { get; set; }
        
        /// <summary>
        /// Lista de comentários recentes no perfil do utilizador
        /// </summary>
        public List<Comment> RecentComments { get; set; }
        
        /// <summary>
        /// Indica se o utilizador autenticado pode avaliar o perfil sendo visualizado
        /// </summary>
        public bool CanRate { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Modelo com os dados de entrada para a edição de perfil
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Ficheiro de imagem para o avatar do utilizador
        /// </summary>
        [BindProperty]
        public IFormFile ImageFile { get; set; }

        /// <summary>
        /// Classe que representa os dados do formulário de edição de perfil
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// Nome completo do utilizador
            /// </summary>
            [Required(ErrorMessage = "O nome é obrigatório")]
            [Display(Name = "Nome Completo")]
            public string FullName { get; set; }

            /// <summary>
            /// Caminho para a imagem de perfil do utilizador
            /// </summary>
            [Display(Name = "Imagem")]
            public string Image { get; set; }

            /// <summary>
            /// Localização/cidade do utilizador
            /// </summary>
            [Display(Name = "Localização")]
            public string Location { get; set; }

            /// <summary>
            /// Descrição/biografia do utilizador
            /// </summary>
            [Display(Name = "Descrição")]
            public string Description { get; set; }

            /// <summary>
            /// Palavra-passe atual do utilizador
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Password atual")]
            public string OldPassword { get; set; }

            /// <summary>
            /// Nova palavra-passe do utilizador
            /// </summary>
            [StringLength(100, ErrorMessage = "A {0} deve ter pelo menos {2} e no máximo {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Nova password")]
            public string NewPassword { get; set; }

            /// <summary>
            /// Confirmação da nova palavra-passe
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirmar nova password")]
            [Compare("NewPassword", ErrorMessage = "A nova password e a confirmação não coincidem.")]
            public string ConfirmPassword { get; set; }
        }

        /// <summary>
        /// Carrega os dados do utilizador para exibição no perfil.
        /// </summary>
        /// <param name="user">Utilizador cujos dados serão carregados</param>
        /// <returns>Tarefa assíncrona</returns>
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

        /// <summary>
        /// Carrega as avaliações do utilizador.
        /// </summary>
        /// <param name="userId">ID do utilizador cujas avaliações serão carregadas</param>
        /// <returns>Tarefa assíncrona</returns>
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

        /// <summary>
        /// Manipula o pedido GET para a página de perfil.
        /// Carrega o perfil do utilizador especificado ou do utilizador atual.
        /// </summary>
        /// <param name="userId">ID do utilizador a ser visualizado (opcional)</param>
        /// <param name="sort">Critério de ordenação para os produtos do utilizador (opcional)</param>
        /// <returns>Página de perfil do utilizador</returns>
        public async Task<IActionResult> OnGetAsync(string userId = null, string sort = null)
        {
            User user;
            CurrentSort = sort;

            if (string.IsNullOrEmpty(userId))
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound($"Não foi possível carregar o utilizador.");
                }
                IsCurrentUser = true;
            }
            else
            {
                user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound($"Não foi possível encontrar o utilizador com ID '{userId}'.");
                }
                IsCurrentUser = User.Identity.IsAuthenticated &&
                    userId == _userManager.GetUserId(User);
            }

            await LoadAsync(user);
            var allUserProducts = await _productRepository.GetUserProductsAsync(user.Id, sort);


            UserProducts = allUserProducts.Where(p => !p.IsDonated).ToList();
            return Page();
        }

        /// <summary>
        /// Manipula o pedido POST para atualização do perfil.
        /// Processa as alterações nos dados do utilizador.
        /// </summary>
        /// <returns>Página de perfil atualizada ou com erros de validação</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Não foi possível carregar o utilizador.");
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
        
        /// <summary>
        /// Manipula o pedido POST para submeter uma avaliação a um perfil.
        /// Adiciona ou atualiza uma avaliação do utilizador atual para o utilizador especificado.
        /// </summary>
        /// <param name="ratedUserId">ID do utilizador a ser avaliado</param>
        /// <param name="rating">Pontuação da avaliação (1-5)</param>
        /// <returns>Redirecionamento para a página de perfil com mensagem de resultado</returns>
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
                return NotFound("Não foi possível carregar o utilizador.");
            }

            if (currentUser.Id == ratedUserId)
            {
                StatusMessage = "Não podes avaliar seu próprio perfil.";
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

        /// <summary>
        /// Manipula o pedido POST para desativar a conta do utilizador atual.
        /// </summary>
        /// <returns>Redirecionamento para a página de login após desativação bem-sucedida</returns>
        public async Task<IActionResult> OnPostDeactivateAccountAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Não foi possível carregar o utilizador.");
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