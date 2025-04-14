// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecondChance.Models;

namespace SecondChance.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Modelo da página de início de sessão.
    /// Gere o processo de autenticação de utilizadores na aplicação.
    /// </summary>
    public class LoginModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;

        /// <summary>
        /// Construtor da classe LoginModel.
        /// </summary>
        /// <param name="signInManager">Gestor de autenticação para operações de início de sessão</param>
        /// <param name="userManager">Gestor de utilizadores que fornece APIs para gerir utilizadores</param>
        public LoginModel(SignInManager<User> signInManager, UserManager<User> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        /// <summary>
        /// Modelo com os dados de entrada para o início de sessão
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Lista de esquemas de autenticação externa disponíveis
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        /// URL de retorno após o início de sessão
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        /// Mensagem de erro temporária para exibição ao utilizador
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Classe que representa os dados do formulário de início de sessão
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// Endereço de email do utilizador
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            /// <summary>
            /// Palavra-passe do utilizador
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            [PasswordStrengthAttribute]
            public string Password { get; set; }

            /// <summary>
            /// Opção para manter a sessão iniciada
            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }

            /// <summary>
            /// Opção para reativar uma conta previamente desativada durante o início de sessão
            /// </summary>
            [Display(Name = "Reativar conta desativada")]
            public bool ReactivateAccount { get; set; }
        }

        /// <summary>
        /// Manipula o pedido GET para a página de início de sessão.
        /// Configura os dados iniciais para o formulário de início de sessão.
        /// </summary>
        /// <param name="returnUrl">URL para redirecionar após o início de sessão</param>
        /// <param name="email">Email do utilizador, se conhecido</param>
        /// <param name="inactive">Indica se a conta está inativa</param>
        public async Task OnGetAsync(string returnUrl = null, string email = null, bool inactive = false)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            if (!string.IsNullOrEmpty(email) && inactive)
            {
                Input = new InputModel
                {
                    ReactivateAccount = true
                };
            }

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        /// <summary>
        /// Manipula o pedido POST para o início de sessão de utilizadores.
        /// Autentica o utilizador se as credenciais forem válidas.
        /// </summary>
        /// <param name="returnUrl">URL para redirecionar após o início de sessão bem-sucedido</param>
        /// <returns>Página inicial ou página de início de sessão com erros de validação</returns>
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);

                if (user != null)
                {
                    if (!await _userManager.IsEmailConfirmedAsync(user))
                    {
                        ModelState.AddModelError(string.Empty, "Precisa de confirmar seu email antes de fazer login.");
                        return Page();
                    }

                    if (user.PermanentlyDisabled)
                    {
                        ModelState.AddModelError(string.Empty, "Esta conta foi permanentemente desativada por um administrador e não pode ser reativada.");
                        return Page();
                    }
                    if (!user.IsActive)
                    {
                        if (Input.ReactivateAccount)
                        {
                            user.IsActive = true;
                            var updateResult = await _userManager.UpdateAsync(user);

                            if (!updateResult.Succeeded)
                            {
                                return Page();
                            }

                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Esta conta foi desativada. Marque a opção abaixo para reativar sua conta.");
                            return Page();
                        }
                    }
                }

                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            return Page();
        }
    }
}
