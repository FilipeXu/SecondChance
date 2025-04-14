// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecondChance.Models;

namespace SecondChance.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Modelo da página de encerramento de sessão.
    /// Gere o processo de término da sessão do utilizador na aplicação.
    /// </summary>
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;

        /// <summary>
        /// Construtor da classe LogoutModel.
        /// </summary>
        /// <param name="signInManager">Gestor de autenticação para operações de início e encerramento de sessão</param>
        public LogoutModel(SignInManager<User> signInManager)
        {
            _signInManager = signInManager;
        }

        /// <summary>
        /// Manipula o pedido POST para o encerramento de sessão de utilizadores.
        /// Termina a sessão atual do utilizador autenticado.
        /// </summary>
        /// <param name="returnUrl">URL para redirecionar após o encerramento de sessão</param>
        /// <returns>Redirecionamento para a URL especificada ou para a página atual</returns>
        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                return RedirectToPage();
            }
        }
    }
}
