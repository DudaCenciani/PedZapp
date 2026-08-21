using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Enums;
using PedZapp.ViewModels.Pedido;

namespace PedZapp.Controllers
{
    /// <summary>
    /// Exibe a confirmação pública de um pedido usando o código público e o slug da empresa.
    /// </summary>
    [AllowAnonymous]
    // Permite que clientes visualizem a confirmação sem uma sessão administrativa.
    [Route("{slug:empresaSlug}/pedido")]
    // Mantém o pedido público abaixo da empresa identificada pelo slug validado pela constraint.
    public class PedidoPublicoController : Controller
    {
        // Contexto EF usado exclusivamente para projetar os dados públicos da confirmação.
        private readonly ApplicationDbContext _context;
        // Recebe o contexto pela injeção de dependência e o armazena no campo do controller.
        public PedidoPublicoController(ApplicationDbContext context) => _context = context;

        /// <summary>
        /// Localiza um pedido pelo código público dentro da empresa do slug e monta a confirmação segura.
        /// </summary>
        /// <param name="slug">Slug que limita a consulta à empresa correta.</param>
        /// <param name="codigo">Código público do pedido a confirmar.</param>
        [HttpGet("{codigo}/confirmacao")]
        // Define a URL pública específica da confirmação do pedido.
        public async Task<IActionResult> Confirmacao(string slug, string codigo)
        {
            // Consulta sem rastreamento e exige simultaneamente o código e o slug da empresa do pedido.
            var pedido = await _context.Pedidos.AsNoTracking()
                .Where(p => p.CodigoPublico == codigo && p.Empresa!.Slug == slug)
                .Select(p => new PedidoConfirmacaoPublicaViewModel
                {
                    Slug = slug,
                    NomeFantasia = p.Empresa!.NomeFantasia,
                    NumeroPedido = p.NumeroPedido,
                    CodigoPublico = p.CodigoPublico,
                    TipoAtendimento = p.TipoAtendimento == TipoAtendimento.Entrega ? "Entrega" : "Retirada",
                    FormaPagamento = p.NomeFormaPagamentoSnapshot,
                    Total = p.Total
                })
                .FirstOrDefaultAsync();

            // Retorna 404 para código inexistente naquele tenant; caso contrário, renderiza somente a projeção pública.
            return pedido is null ? NotFound() : View(pedido);
        }
    }
}
