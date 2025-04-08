// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecondChance.Models;

namespace SecondChance.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IUserStore<User> _userStore;
        private readonly IUserEmailStore<User> _emailStore;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ExternalLoginModel> _logger;

        public ExternalLoginModel(
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            IUserStore<User> userStore,
            ILogger<ExternalLoginModel> logger,
            IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _logger = logger;
            _emailSender = emailSender;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ProviderDisplayName { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            [Required(ErrorMessage = "O nome completo é obrigatório")]
            [Display(Name = "Nome Completo")]
            public string FullName { get; set; }

            [Required(ErrorMessage = "A data de nascimento é obrigatória")]
            [DataType(DataType.Date)]
            [MaximumAge(125, ErrorMessage = "Idade máxima permitida é 125 anos")]
            [MinimumAge(18, ErrorMessage = "Deve ter pelo menos 18 anos para se registar")]
            [Display(Name = "Data de Nascimento")]
            public DateTime BirthDate { get; set; }
            
            [Required(ErrorMessage = "O email é obrigatório")]
            [EmailAddress(ErrorMessage = "Email inválido")]
            [Display(Name = "Email")]
            public string Email { get; set; }
            
            /// <summary>
            /// Option to reactivate a previously deactivated account during login
            /// </summary>
            [Display(Name = "Reativar conta desativada")]
            public bool ReactivateAccount { get; set; }
        }

        public IActionResult OnGet() => RedirectToPage("./Login");

        public IActionResult OnPost(string provider, string returnUrl = null, bool reactivate = false)
        {
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl, reactivate });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null, bool reactivate = false)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            if (remoteError != null)
            {
                ErrorMessage = $"Erro do provedor externo: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }
            
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Erro ao carregar informações de login externo.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (!string.IsNullOrEmpty(email))
            {
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    if (existingUser.PermanentlyDisabled)
                    {
                        ErrorMessage = "Esta conta foi permanentemente desativada por um administrador e não pode ser reativada.";
                        return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                    }

                    if (!existingUser.IsActive)
                    {
                        if (reactivate)
                        {
                            existingUser.IsActive = true;
                            var updateResult = await _userManager.UpdateAsync(existingUser);
                            
                            if (updateResult.Succeeded)
                            {
                                await _signInManager.SignInAsync(existingUser, isPersistent: false, info.LoginProvider);
                                return LocalRedirect(returnUrl);
                            }
                            else
                            {
                                foreach (var error in updateResult.Errors)
                                {
                                    ModelState.AddModelError(string.Empty, error.Description);
                                }
                                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                            }
                        }
                        else
                        {
                            ErrorMessage = "Esta conta foi desativada. Por favor, marque a opção para reativá-la e marque a opção de external login desejado.";
                            return RedirectToPage("./Login", new { email = email, inactive = true, ReturnUrl = returnUrl });
                        }
                    }
                }
            }

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("{Name} fez login com o provedor {LoginProvider}.", info.Principal.Identity.Name, info.LoginProvider);
                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }
            else
            {
                ReturnUrl = returnUrl;
                ProviderDisplayName = info.ProviderDisplayName;
                if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
                {
                    Input = new InputModel { Email = info.Principal.FindFirstValue(ClaimTypes.Email) };
                }
                return Page();
            }
        }

        public async Task<IActionResult> OnPostCallbackAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            if (!string.IsNullOrEmpty(TempData["UserEmail"] as string) && Input.ReactivateAccount)
            {
                var email = TempData["UserEmail"] as string;
                var user = await _userManager.FindByEmailAsync(email);
                
                if (user != null && !user.PermanentlyDisabled)
                {
                    user.IsActive = true;
                    var updateResult = await _userManager.UpdateAsync(user);
                    
                    if (updateResult.Succeeded)
                    {
                        var info = await _signInManager.GetExternalLoginInfoAsync();
                        if (info != null)
                        {
                            await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                            return LocalRedirect(returnUrl);
                        }
                    }
                }
            }
            
            var externalInfo = await _signInManager.GetExternalLoginInfoAsync();
            if (externalInfo == null)
            {
                ErrorMessage = "Erro ao processar o login externo.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }
            
            ReturnUrl = returnUrl;
            ProviderDisplayName = externalInfo.ProviderDisplayName;
            return Page();
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Erro ao carregar informações de login externo durante a confirmação.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(Input.Email);
                if (existingUser != null)
                {
                    var existingLogins = await _userManager.GetLoginsAsync(existingUser);
                    var existingLoginForProvider = existingLogins.FirstOrDefault(
                        el => el.LoginProvider == info.LoginProvider);
                    
                    if (existingLoginForProvider != null)
                    {
                        await _signInManager.SignInAsync(existingUser, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                    
                    ModelState.AddModelError("Input.Email", "Este email já está registrado. Por favor, faça login com sua senha ou utilize outro email.");
                    return Page();
                }

                var user = CreateUser();
                user.Location = "Por definir";
                user.JoinDate = DateTime.Now;
                user.Image = "Por definir";
                user.PhoneNumber = "Por definir";
                user.Description = "Escreva algo sobre si e o seu numero de telemóvel...";
                user.IsActive = true;
                user.FullName = Input.FullName;
                user.BirthDate = Input.BirthDate;
                user.PermanentlyDisabled = false;
                user.EmailConfirmed = true;

                bool isFirstUser = !_userManager.Users.Any();
                if (isFirstUser)
                {
                    user.IsAdmin = true;
                    user.IsFirstUser = true;
                }

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        
                        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ProviderDisplayName = info.ProviderDisplayName;
            ReturnUrl = returnUrl;
            return Page();
        }

        private User CreateUser()
        {
            try
            {
                return Activator.CreateInstance<User>();
            }
            catch
            {
                throw new InvalidOperationException($"Não foi possível criar uma instância de '{nameof(User)}'. " +
                    $"Certifique-se de que '{nameof(User)}' não é uma classe abstrata e tem um construtor sem parâmetros, ou " +
                    $"substitua a página de login externo em /Areas/Identity/Pages/Account/ExternalLogin.cshtml");
            }
        }

        private IUserEmailStore<User> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("A UI padrão requer uma loja de usuário com suporte a e-mail.");
            }
            return (IUserEmailStore<User>)_userStore;
        }
    }
}