// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using SecondChance.Models;

namespace SecondChance.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Modelo da página de recuperação de palavra-passe.
    /// Gere o processo de envio de links para redefinição de palavra-passe.
    /// </summary>
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;

        /// <summary>
        /// Construtor da classe ForgotPasswordModel.
        /// </summary>
        /// <param name="userManager">Gestor de utilizadores que fornece APIs para gerir utilizadores</param>
        /// <param name="emailSender">Serviço para envio de emails</param>
        public ForgotPasswordModel(UserManager<User> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        /// <summary>
        /// Modelo com os dados de entrada para a recuperação de palavra-passe
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Classe que representa os dados do formulário de recuperação de palavra-passe
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// Endereço de email do utilizador para recuperação de palavra-passe
            /// </summary>
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }
        }

        /// <summary>
        /// Manipula o pedido POST para a recuperação de palavra-passe.
        /// Envia um email com link para redefinição se o email for válido.
        /// </summary>
        /// <returns>Redirecionamento para a página de confirmação</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Recuperação de Password",
                    $"Por favor, redefina sua senha <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicando aqui</a>.");

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}
