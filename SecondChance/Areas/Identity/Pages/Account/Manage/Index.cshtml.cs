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

namespace SecondChance.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public IndexModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public User UserProfile { get; set; }
        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }
        public bool HasPassword { get; set; }

        public IFormFile ImageFile { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "username")]
            [Display(Name = "Username")]
            public string FullName { get; set; }

            [Phone]
            [Display(Name = "Número de telefone")]
            public string PhoneNumber { get; set; }

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
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            HasPassword = await _userManager.HasPasswordAsync(user);


            Input = new InputModel
            {
                FullName = user.FullName,
                PhoneNumber = phoneNumber,
                Image = user.Image,
                Location = user.Location,
                Description = user.Description
            };

            UserProfile = user;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }
            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Erro inesperado ao tentar atualizar o número de telefone.";
                    return RedirectToPage();
                }
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
                return RedirectToPage();
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
                else
                {
                    await _signInManager.RefreshSignInAsync(user);
                    StatusMessage = "Seu perfil e senha foram atualizados com sucesso.";
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Seu perfil foi atualizado com sucesso";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeactivateAccountAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
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