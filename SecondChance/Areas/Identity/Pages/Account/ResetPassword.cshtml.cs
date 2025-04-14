using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using SecondChance.Models;

namespace SecondChance.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Modelo da página de redefinição de palavra-passe.
    /// Gere o processo de alteração de palavra-passe após recuperação.
    /// </summary>
    [AllowAnonymous]
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<User> _userManager;

        /// <summary>
        /// Construtor da classe ResetPasswordModel.
        /// </summary>
        /// <param name="userManager">Gestor de utilizadores que fornece APIs para gerir utilizadores</param>
        public ResetPasswordModel(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Modelo com os dados de entrada para a redefinição de palavra-passe
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Classe que representa os dados do formulário de redefinição de palavra-passe
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
            /// Nova palavra-passe do utilizador
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            [PasswordStrengthAttribute]
            public string Password { get; set; }

            /// <summary>
            /// Confirmação da nova palavra-passe
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirmar senha")]
            [Compare("Password", ErrorMessage = "A senha e a senha de confirmação não correspondem.")]
            public string ConfirmPassword { get; set; }

            /// <summary>
            /// Código de verificação para redefinição de palavra-passe
            /// </summary>
            [Required]
            public string Code { get; set; }
        }

        /// <summary>
        /// Manipula o pedido GET para a página de redefinição de palavra-passe.
        /// Configura o código de verificação para o formulário.
        /// </summary>
        /// <param name="code">Código de verificação para redefinição de palavra-passe</param>
        /// <returns>Página de redefinição de palavra-passe ou erro se o código for inválido</returns>
        public IActionResult OnGet(string code = null)
        {
            if (code == null)
            {
                return BadRequest("É necessário um código para redefinir a senha.");
            }
            else
            {
                Input = new InputModel
                {
                    Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code))
                };
                return Page();
            }
        }

        /// <summary>
        /// Manipula o pedido POST para a redefinição de palavra-passe.
        /// Altera a palavra-passe do utilizador se o código for válido.
        /// </summary>
        /// <returns>Redirecionamento para a página de início de sessão ou página com erros de validação</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                return RedirectToPage("./Login");
            }

            var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
            if (result.Succeeded)
            {
                return RedirectToPage("./Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }
    }
}