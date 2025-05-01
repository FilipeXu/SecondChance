using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SecondChance.Models;

namespace SecondChance.Controllers
{
    /// <summary>
    /// Controlador responsável pela gestão das páginas principais da aplicação.
    /// Implementa funcionalidades para a página inicial e de privacidade.
    /// </summary>
    public class HomeController : Controller
    {

        /// <summary>
        /// Construtor do HomeController.
        /// </summary>
        public HomeController()
        {
        }

        /// <summary>
        /// Apresenta a página inicial da aplicação.
        /// </summary>
        /// <returns>Vista da página inicial</returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Apresenta a página de política de privacidade.
        /// </summary>
        /// <returns>Vista da página de privacidade</returns>
        public IActionResult Privacy()
        {
            return View();
        }
    }
}
