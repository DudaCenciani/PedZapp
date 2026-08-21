using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedZapp.Data;

namespace PedZapp.Controllers
{
    /// <summary>
    /// Resolve uma empresa pelo slug para a tela pública legada da loja.
    /// </summary>
    public class LojaController : Controller
    {
        // Contexto EF utilizado somente para consultar a empresa solicitada.
        private readonly ApplicationDbContext _context;

        // Recebe o contexto configurado pela injeção de dependência.
        public LojaController(ApplicationDbContext context)
        {
            // Mantém a dependência disponível para as actions deste controller.
            _context = context;
        }

        /// <summary>
        /// Busca a empresa cujo slug foi recebido na rota e exibe seu modelo quando ela existe.
        /// </summary>
        /// <param name="slug">Identificador textual recebido pela rota da loja.</param>
        public async Task<IActionResult> Index(string slug)
        {
            // Consulta assíncrona da empresa correspondente exatamente ao slug informado.
            var empresa =
                await _context.Empresas
                .FirstOrDefaultAsync(e => e.Slug == slug);

            // Distingue uma loja inexistente de uma loja válida sem expor outra empresa.
            if (empresa == null)
                return NotFound();

            // Entrega à View somente a empresa encontrada pela consulta acima.
            return View(empresa);
        }
    }
}
