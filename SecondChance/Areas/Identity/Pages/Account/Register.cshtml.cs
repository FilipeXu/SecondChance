// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using SecondChance.Models;

namespace SecondChance.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Modelo da página de registo de utilizadores.
    /// Gere o processo de criação de novas contas de utilizadores na aplicação.
    /// </summary>
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IUserStore<User> _userStore;
        private readonly IUserEmailStore<User> _emailStore;
        private readonly IEmailSender _emailSender;

        /// <summary>
        /// Construtor da classe RegisterModel.
        /// </summary>
        /// <param name="userManager">Gestor de utilizadores que fornece APIs para gerir utilizadores</param>
        /// <param name="userStore">Armazenamento responsável pelos dados dos utilizadores</param>
        /// <param name="signInManager">Gestor de autenticação para operações de início de sessão</param>
        /// <param name="emailSender">Serviço para envio de emails</param>
        public RegisterModel(
            UserManager<User> userManager,
            IUserStore<User> userStore,
            SignInManager<User> signInManager,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        /// <summary>
        /// Modelo com os dados de entrada para o registo
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// URL de retorno após o registo
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        /// Lista de esquemas de autenticação externa disponíveis
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        /// Classe que representa os dados do formulário de registo
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// Nome completo do utilizador
            /// </summary>
            [Required]
            [Display(Name = "Nome Completo")]
            public string FullName { get; set; }

            /// <summary>
            /// Data de nascimento do utilizador
            /// </summary>
            [Required]
            [DataType(DataType.Date)]
            [Display(Name = "Data de Nascimento")]
            [MinimumAge(18)]
            [MaximumAge(125)]
            public DateTime BirthDate { get; set; }

            /// <summary>
            /// Endereço de email do utilizador
            /// </summary>
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
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
            /// Confirmação da palavra-passe
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        /// <summary>
        /// Manipula o pedido GET para a página de registo.
        /// Carrega os métodos de autenticação externa disponíveis.
        /// </summary>
        /// <param name="returnUrl">URL para redirecionar após o registo</param>
        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        /// <summary>
        /// Manipula o pedido POST para o registo de utilizadores.
        /// Cria um novo utilizador se os dados forem válidos.
        /// </summary>
        /// <param name="returnUrl">URL para redirecionar após o registo bem-sucedido</param>
        /// <returns>Página de confirmação, página inicial ou página de registo com erros de validação</returns>
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(Input.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Input.Email", "Este email já existe.");
                    return Page();
                }                var user = CreateUser();
                user.Location = "Por definir";
                user.JoinDate = DateTime.Now;
                user.Image = "/Images/Default.jpg";
                user.PhoneNumber = "Por definir";
                user.Description = "Escreva algo sobre si e o seu numero de telemóvel...";
                user.IsActive = true;
                user.FullName = Input.FullName;
                user.BirthDate = Input.BirthDate;
                user.PermanentlyDisabled = false;

                bool isFirstUser = !_userManager.Users.Any();
                if (isFirstUser)
                {
                    user.IsAdmin = true;
                    user.IsFirstUser = true;
                }

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }

        /// <summary>
        /// Cria uma nova instância da classe User.
        /// </summary>
        /// <returns>Nova instância da classe User</returns>
        /// <exception cref="InvalidOperationException">Ocorre se não for possível criar uma instância da classe User</exception>
        private User CreateUser()
        {
            try
            {
                return Activator.CreateInstance<User>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(User)}'. " +
                    $"Ensure that '{nameof(User)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        /// <summary>
        /// Obtém o armazenamento de emails para utilizadores.
        /// </summary>
        /// <returns>Interface para gerir emails de utilizadores</returns>
        /// <exception cref="NotSupportedException">Ocorre se o gestor de utilizadores não suportar email</exception>
        private IUserEmailStore<User> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<User>)_userStore;
        }
    }
}