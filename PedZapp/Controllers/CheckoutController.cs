using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Services;
using PedZapp.ViewModels.Checkout;
using PedZapp.ViewModels.Pedido;

namespace PedZapp.Controllers
{
    /// <summary>
    /// Exibe as opções públicas de checkout e delega a criação do pedido ao PedidoService.
    /// O controller não calcula valores: o serviço revalida catálogo, taxa, pagamento e total no servidor.
    /// </summary>
    [AllowAnonymous]
    // Permite que o cliente conclua um pedido público sem sessão administrativa.
    [Route("{slug:empresaSlug}/checkout")]
    // Mantém o checkout associado ao slug validado da empresa.
    public class CheckoutController : Controller
    {
        // Contexto usado para projetar bairros e formas de pagamento públicas.
        private readonly ApplicationDbContext _context;
        // Serviço que revalida catálogo, valores e cria o pedido no servidor.
        private readonly IPedidoService _pedidoService;
        // Centraliza a mesma disponibilidade operacional exibida no painel e cardápio público.
        private readonly IStatusLojaService _statusLoja;

        public CheckoutController(ApplicationDbContext context, IPedidoService pedidoService, IStatusLojaService statusLoja)
        {
            // Armazena o contexto de leitura injetado.
            _context = context;
            // Armazena o serviço responsável pela criação segura do pedido.
            _pedidoService = pedidoService;
            _statusLoja = statusLoja;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string slug)
        {
            // Localiza a empresa pelo slug e projeta somente os dados necessários ao checkout público.
            var empresa = await _context.Empresas.AsNoTracking()
                .Where(e => e.Slug == slug)
                .Select(e => new EmpresaCheckoutConsulta
                {
                    Id = e.Id,
                    Slug = e.Slug,
                    NomeFantasia = e.NomeFantasia,
                    Ativa = e.Ativa,
                    CardapioPublicado = e.ConfiguracaoLoja != null && e.ConfiguracaoLoja.CardapioPublicado,
                    CorPrimaria = e.ConfiguracaoLoja!.CorPrimaria ?? "#F6C445",
                    CorSecundaria = e.ConfiguracaoLoja!.CorSecundaria ?? "#C98D86"
                })
                .FirstOrDefaultAsync();

            // Trata slug inexistente sem revelar dados internos.
            if (empresa is null)
                return NotFound();

            // Mantém o checkout coerente com o painel e o cardápio para empresa inativa, cardápio pausado ou horário fechado.
            if (!(await _statusLoja.ObterStatusAsync(empresa.Id)).Aberta)
                return View("~/Views/Cardapio/Indisponivel.cshtml");

            // Consulta bairros ativos exclusivamente da empresa localizada.
            var bairros = await _context.BairrosEntrega.AsNoTracking()
                .Where(b => b.EmpresaId == empresa.Id && b.Ativo)
                .OrderBy(b => b.OrdemExibicao).ThenBy(b => b.NomeBairro)
                .Select(b => new BairroCheckoutViewModel
                {
                    Id = b.Id,
                    Nome = b.NomeBairro,
                    TaxaEntrega = b.TaxaEntrega,
                    TempoEstimadoEntregaMinutos = b.TempoEstimadoEntregaMinutos,
                    PedidoMinimo = b.PedidoMinimo
                })
                .ToListAsync();

            // Consulta formas de pagamento ativas exclusivamente da mesma empresa.
            var pagamentos = await _context.FormasPagamento.AsNoTracking()
                .Where(f => f.EmpresaId == empresa.Id && f.Ativa)
                .OrderBy(f => f.OrdemExibicao).ThenBy(f => f.Nome)
                .Select(f => new FormaPagamentoCheckoutViewModel
                {
                    Id = f.Id,
                    Nome = f.Nome,
                    Tipo = (int)f.Tipo,
                    AceitaTroco = f.AceitaTroco,
                    PagamentoNaEntrega = f.PagamentoNaEntrega,
                    Observacao = f.Observacao
                })
                .ToListAsync();

            // Entrega à View um modelo público com as opções filtradas pelo tenant.
            return View(new CheckoutPublicoViewModel
            {
                Slug = empresa.Slug,
                NomeFantasia = empresa.NomeFantasia,
                CorPrimaria = empresa.CorPrimaria,
                CorSecundaria = empresa.CorSecundaria,
                Bairros = bairros,
                FormasPagamento = pagamentos
            });
        }

        [HttpPost("finalizar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Finalizar(string slug, FinalizarPedidoRequestVM request)
        {
            // Rejeita a entrada inválida antes de chamar a criação de pedido.
            if (!ModelState.IsValid)
                return BadRequest(new { erro = "Não foi possível validar os dados enviados." });

            // Delega ao serviço o recálculo no servidor e a criação segura do pedido.
            var resultado = await _pedidoService.CriarAsync(slug, request);
            if (resultado.SlugNaoEncontrado)
                return NotFound();
            if (!resultado.Sucesso)
                return BadRequest(new { erro = resultado.Erro ?? "Não foi possível registrar o pedido." });

            // Retorna ao cliente somente a URL pública de confirmação do pedido criado.
            return Ok(new
            {
                redirectUrl = Url.Action("Confirmacao", "PedidoPublico", new { slug, codigo = resultado.CodigoPublico })
            });
        }

        // Projeção privada com os campos de empresa necessários exclusivamente no checkout.
        private sealed class EmpresaCheckoutConsulta
        {
            public int Id { get; init; }
            public string Slug { get; init; } = string.Empty;
            public string NomeFantasia { get; init; } = string.Empty;
            public bool Ativa { get; init; }
            public bool CardapioPublicado { get; init; }
            public string CorPrimaria { get; init; } = "#F6C445";
            public string CorSecundaria { get; init; } = "#C98D86";
        }
    }
}
