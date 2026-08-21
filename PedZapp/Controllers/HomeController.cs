using Microsoft.AspNetCore.Mvc;

namespace PedZapp.Controllers
{
    /// <summary>
    /// Disponibiliza a página usada pelo cookie de autenticação quando uma identidade não possui permissão.
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// Exibe a tela informativa de acesso negado, sem alterar a decisão de autorização já tomada pelo pipeline.
        /// </summary>
        public IActionResult AccessDenied()
        {
            // Retorna a View convencional AccessDenied associada a esta action.
            return View();
        }
    }
}
