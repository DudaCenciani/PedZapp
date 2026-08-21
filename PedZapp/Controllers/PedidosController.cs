using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Enums;
using PedZapp.Models;
using PedZapp.Services;
using PedZapp.ViewModels.Pedido;

namespace PedZapp.Controllers
{
    /// <summary>
    /// Gerencia pedidos administrativos da empresa autenticada, incluindo criação manual,
    /// transições de status e solicitações de impressão. Todas as ações validam slug e EmpresaId antes do acesso.
    /// </summary>
    [Authorize]
    // Exige autenticação antes de manipular pedidos administrativos.
    [Route("{slug}/pedidos")]
    // Mantém o slug da empresa como contexto obrigatório do módulo.
    public class PedidosController : Controller
    {
        // Contexto EF para consultas de pedido sempre filtradas pela empresa.
        private readonly ApplicationDbContext _context;
        // Resolve o usuário da sessão e seu EmpresaId.
        private readonly UserManager<ApplicationUser> _users;
        // Serviço que aplica transições válidas de status.
        private readonly IPedidoStatusService _statusService;
        // Serviço que recalcula e cria pedidos manualmente no servidor.
        private readonly IPedidoService _pedidoService;
        // Serviço que prepara e registra impressões de pedidos.
        private readonly IPedidoPrintService _printService;
        // Serviço que publica avisos pós-commit e avisos fictícios de Development.
        private readonly IPedidoNotificacaoService _notificacoes;
        // Orquestra a confirmação opcional pela Cloud API após o status já ter sido salvo.
        private readonly IPedidoWhatsAppNotificacaoService _whatsAppNotificacoes;
        // Impede que a rota de teste exista funcionalmente fora do ambiente de desenvolvimento.
        private readonly IWebHostEnvironment _environment;

        public PedidosController(ApplicationDbContext context, UserManager<ApplicationUser> users, IPedidoStatusService statusService, IPedidoService pedidoService, IPedidoPrintService printService, IPedidoNotificacaoService notificacoes, IPedidoWhatsAppNotificacaoService whatsAppNotificacoes, IWebHostEnvironment environment)
        {
            // Armazena todas as dependências injetadas para as actions do módulo.
            _context = context;
            _users = users;
            _statusService = statusService;
            _pedidoService = pedidoService;
            _printService = printService;
            _notificacoes = notificacoes;
            _whatsAppNotificacoes = whatsAppNotificacoes;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string slug, string? busca, StatusPedido? status, DateTime? dataInicial)
        {
            // Autoriza o tenant antes de aplicar filtros administrativos na lista.
            var acesso = await AcessoAsync(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            return View(await CriarIndexAsync(acesso.Empresa!, busca, status, dataInicial));
        }

        [HttpGet("novo")]
        public async Task<IActionResult> Novo(string slug)
        {
            // Autoriza a empresa antes de carregar o catálogo do pedido manual.
            var acesso = await AcessoAsync(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            return View(await CriarPedidoManualAsync(acesso.Empresa!));
        }

        [HttpPost("novo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Novo(string slug, FinalizarPedidoRequestVM request)
        {
            // Autoriza a empresa antes de aceitar a solicitação de pedido manual.
            var acesso = await AcessoAsync(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            if (!ModelState.IsValid) return BadRequest(new { erro = "Não foi possível validar os dados do pedido." });

            // Delega ao serviço o recálculo e a criação usando o slug já autorizado.
            var resultado = await _pedidoService.CriarAsync(acesso.Empresa!.Slug, request, OrigemPedido.Manual);
            if (resultado.SlugNaoEncontrado) return NotFound();
            if (!resultado.Sucesso) return BadRequest(new { erro = resultado.Erro ?? "Não foi possível criar o pedido." });

            TempData["Sucesso"] = "Pedido criado com sucesso.";
            return Ok(new { redirectUrl = Url.Action(nameof(Index), new { slug = acesso.Empresa.Slug }) });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Detalhes(string slug, int id)
        {
            // Autoriza a empresa antes de carregar os detalhes do pedido solicitado.
            var acesso = await AcessoAsync(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            // Projeta detalhes somente quando Id e EmpresaId correspondem ao tenant atual.
            var pedido = await _context.Pedidos.AsNoTracking().Where(p => p.Id == id && p.EmpresaId == acesso.Empresa!.Id)
                .Select(p => new PedidoDetalhesViewModel
                {
                    Slug = acesso.Empresa!.Slug, Id = p.Id, NumeroPedido = p.NumeroPedido, Status = p.Status, DataCriacao = p.DataCriacao,
                    CodigoPublico = p.CodigoPublico,
                    NomeCliente = p.NomeCliente, TelefoneCliente = p.TelefoneCliente, TipoAtendimento = p.TipoAtendimento,
                    Bairro = p.NomeBairroSnapshot, TaxaEntrega = p.TaxaEntrega, Rua = p.Rua, NumeroEndereco = p.NumeroEndereco,
                    Complemento = p.Complemento, Referencia = p.Referencia, FormaPagamento = p.NomeFormaPagamentoSnapshot,
                    PrecisaTroco = p.PrecisaTroco, TrocoPara = p.TrocoPara, Subtotal = p.Subtotal, Total = p.Total, Pago = p.Pago,
                    AceitaAtualizacoesWhatsApp = p.AceitaAtualizacoesWhatsApp, WhatsAppConfirmacaoEnviadaEm = p.WhatsAppConfirmacaoEnviadaEm,
                    WhatsAppConfirmacaoFalhouEm = p.WhatsAppConfirmacaoFalhouEm,
                    Itens = p.Itens.Select(i => new PedidoDetalheItemViewModel
                    {
                        Nome = i.NomeProdutoSnapshot, Quantidade = i.Quantidade, PrecoUnitario = i.PrecoUnitario, Subtotal = i.Subtotal, Observacao = i.Observacao,
                        Adicionais = i.Adicionais.Select(a => new PedidoDetalheAdicionalViewModel { Nome = a.NomeAdicionalSnapshot, PrecoUnitario = a.PrecoUnitario, Quantidade = a.Quantidade }).ToList()
                    }).ToList()
                }).FirstOrDefaultAsync();
            if (pedido is null) return NotFound();
            pedido.UltimaImpressaoStatus = await _context.ImpressaoPedidos.AsNoTracking().Where(i => i.PedidoId == pedido.Id && i.EmpresaId == acesso.Empresa!.Id)
                .OrderByDescending(i => i.DataCriacao).Select(i => (StatusImpressao?)i.StatusImpressao).FirstOrDefaultAsync();
            return View(pedido);
        }

        [HttpGet("{codigoPublico}/imprimir")]
        public async Task<IActionResult> Imprimir(string slug, string codigoPublico, string impressao)
        {
            // Autoriza a empresa e exige o token de impressão antes de produzir a página imprimível.
            var acesso = await AcessoAsync(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            if (string.IsNullOrWhiteSpace(impressao)) return NotFound();
            var model = await _printService.ObterParaImpressaoAsync(acesso.Empresa!.Id, codigoPublico, impressao);
            return model is null ? NotFound() : View(model);
        }

        [HttpPost("{codigoPublico}/imprimir/{tokenPublico}/concluir")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConcluirImpressao(string slug, string codigoPublico, string tokenPublico)
        {
            // Registra a tentativa de impressão somente na empresa autorizada.
            var acesso = await AcessoAsync(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            return await _printService.RegistrarTentativaAsync(acesso.Empresa!.Id, codigoPublico, tokenPublico) ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/imprimir/{tipo}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SolicitarImpressao(string slug, int id, TipoImpressao tipo)
        {
            // Autoriza o tenant e valida o enum de impressão antes de criar a reimpressão.
            var acesso = await AcessoAsync(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            if (!Enum.IsDefined(tipo)) return NotFound();
            var resultado = await _printService.CriarReimpressaoAsync(id, acesso.Empresa!.Id, tipo);
            if (resultado.PedidoNaoEncontrado) return NotFound();
            if (!resultado.Sucesso) { TempData["Erro"] = resultado.Erro ?? "Não foi possível preparar a impressão."; return RedirectToAction(nameof(Detalhes), new { slug = acesso.Empresa.Slug, id }); }
            return RedirectToAction(nameof(Imprimir), new { slug = acesso.Empresa.Slug, codigoPublico = resultado.CodigoPublico, impressao = resultado.TokenPublico });
        }

        [HttpPost("{id:int}/avancar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Avancar(string slug, int id)
        {
            // Autoriza a empresa antes de solicitar a próxima transição do pedido.
            var acesso = await AcessoAsync(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            // Lê o fluxo persistido da empresa; a requisição não escolhe a regra de transição.
            var empresa = acesso.Empresa!;
            var fluxo = await _context.ConfiguracoesLoja.AsNoTracking().Where(c => c.EmpresaId == empresa.Id).Select(c => (TipoFluxoPedido?)c.TipoFluxoPedido).FirstOrDefaultAsync() ?? TipoFluxoPedido.Completo;
            var resultado = await _statusService.AvancarAsync(id, empresa.Id, fluxo);
            if (resultado.PedidoNaoEncontrado) return NotFound();
            // A confirmação inicial imprime no fluxo completo e no simplificado, que já entra em preparo.
            if (resultado.Sucesso && resultado.StatusAnterior == StatusPedido.Novo && (resultado.StatusAtual == StatusPedido.Confirmado || resultado.StatusAtual == StatusPedido.EmPreparo))
            {
                var impressao = await _printService.CriarParaConfirmacaoAsync(id, empresa.Id);
                // O status foi persistido pelo serviço antes da impressão. A chamada externa nunca participa dessa transação.
                var whatsApp = await _whatsAppNotificacoes.EnviarConfirmacaoAsync(id, empresa.Id);
                if (impressao.Sucesso)
                {
                    var printUrl = Url.Action(nameof(Imprimir), new { slug = empresa.Slug, codigoPublico = impressao.CodigoPublico, impressao = impressao.TokenPublico });
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Ok(new { redirectUrl = Url.Action(nameof(Index), new { slug = empresa.Slug }), printUrl, whatsAppEnviado = whatsApp.Enviado });
                    return Redirect(printUrl!);
                }
                TempData[whatsApp.Enviado ? "Sucesso" : "Erro"] = whatsApp.Enviado
                    ? "Pedido confirmado e mensagem do WhatsApp enviada, mas não foi possível preparar a impressão."
                    : "Pedido confirmado, mas não foi possível preparar a impressão.";
                return RedirectToAction(nameof(Index), new { slug = empresa.Slug });
            }
            TempData[resultado.Sucesso ? "Sucesso" : "Erro"] = resultado.Sucesso ? "Status do pedido atualizado." : resultado.Erro;
            return RedirectToAction(nameof(Index), new { slug = empresa.Slug });
        }

        [HttpPost("{id:int}/whatsapp/reenvio")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReenviarWhatsApp(string slug, int id)
        {
            // A autorização pelo slug vem antes do serviço, e o serviço repete o filtro EmpresaId em todas as consultas.
            var acesso = await AcessoAsync(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            var empresa = acesso.Empresa!;
            // O endpoint aceita nova tentativa somente para uma falha registrada; não vira um botão de disparos repetidos.
            var pedido = await _context.Pedidos.AsNoTracking()
                .Where(p => p.Id == id && p.EmpresaId == empresa.Id)
                .Select(p => new { p.WhatsAppConfirmacaoFalhouEm })
                .FirstOrDefaultAsync();
            if (pedido is null) return NotFound();
            if (!pedido.WhatsAppConfirmacaoFalhouEm.HasValue)
            {
                TempData["Erro"] = "Esta confirmação não está disponível para reenvio.";
                return RedirectToAction(nameof(Detalhes), new { slug = empresa.Slug, id });
            }
            var resultado = await _whatsAppNotificacoes.EnviarConfirmacaoAsync(id, empresa.Id);
            TempData[resultado.Enviado ? "Sucesso" : "Erro"] = resultado.Enviado
                ? "Mensagem do WhatsApp enviada com sucesso."
                : resultado.Ignorado ? "Esta confirmação não está disponível para reenvio." : resultado.Mensagem;
            return RedirectToAction(nameof(Detalhes), new { slug = empresa.Slug, id });
        }

        [HttpPost("{id:int}/cancelar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(string slug, int id)
        {
            // Autoriza a empresa antes de cancelar o pedido identificado na rota.
            var acesso = await AcessoAsync(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            var resultado = await _statusService.CancelarAsync(id, acesso.Empresa!.Id);
            if (resultado.PedidoNaoEncontrado) return NotFound();
            TempData[resultado.Sucesso ? "Sucesso" : "Erro"] = resultado.Sucesso ? "Pedido cancelado." : resultado.Erro;
            return RedirectToAction(nameof(Index), new { slug = acesso.Empresa.Slug });
        }

        /// <summary>
        /// Publica um aviso fictício para validar SignalR, toast, som e contador sem criar um pedido real.
        /// A action só fica operacional em Development e ainda exige a autorização normal da empresa pelo slug.
        /// </summary>
        [HttpPost("teste-aviso")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TesteAviso(string slug)
        {
            // Production nunca disponibiliza um mecanismo de emissão manual de avisos.
            if (!_environment.IsDevelopment())
                return NotFound();

            var acesso = await AcessoAsync(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;

            // EmpresaId vem do acesso validado, nunca de um campo enviado pelo navegador.
            await _notificacoes.NotificarTesteAsync(acesso.Empresa!.Id, acesso.Empresa.Slug);
            return Ok(new { success = true });
        }

        private async Task<(Empresa? Empresa, IActionResult? Resultado)> AcessoAsync(string slug)
        {
            // NotFound, Challenge e Forbid distinguem slug inexistente, sessão ausente e tentativa entre empresas.
            var empresa = await _context.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Slug == slug);
            if (empresa is null) return (null, NotFound());
            var usuario = await _users.GetUserAsync(User);
            if (usuario is null) return (null, Challenge());
            return usuario.EmpresaId == empresa.Id ? (empresa, null) : (null, Forbid());
        }

        private async Task<PedidosIndexViewModel> CriarIndexAsync(Empresa empresa, string? busca, StatusPedido? status, DateTime? dataInicial)
        {
            // A operação usa DataCriacao, o marco imutável de cada pedido também utilizado nos relatórios do projeto.
            // Sem filtros, o quadro mostra somente o movimento do dia UTC atual, coerente com os pedidos gravados em UTC.
            var hoje = DateTime.UtcNow.Date;
            // dataInicial é o único filtro de data da tela operacional e representa o dia selecionado pelo usuário.
            var dataOperacional = dataInicial?.Date ?? hoje;
            var inicio = dataOperacional;
            // O próximo marco exclusivo mantém o filtro de um dia performático e compatível com SQL.
            var fim = dataOperacional.AddDays(1);
            // Inicia a consulta sempre restringindo pedidos à empresa autorizada.
            var query = _context.Pedidos.AsNoTracking().Where(p => p.EmpresaId == empresa.Id);
            if (!string.IsNullOrWhiteSpace(busca)) query = query.Where(p => p.NumeroPedido.Contains(busca) || p.NomeCliente.Contains(busca) || p.TelefoneCliente.Contains(busca));
            if (status.HasValue) query = query.Where(p => p.Status == status);
            // O intervalo é traduzido pelo EF para SQL e combina data, empresa, busca e status sem materializar histórico.
            query = query.Where(p => p.DataCriacao >= inicio && p.DataCriacao < fim);

            // Define o marco de hoje usado no resumo operacional.
            var pedidos = await query.OrderBy(p => p.Status == StatusPedido.Novo ? 0 : 1).ThenBy(p => p.DataCriacao)
                .Select(p => new PedidoCardViewModel
                {
                    Id = p.Id, NumeroPedido = p.NumeroPedido, NomeCliente = p.NomeCliente, DataCriacao = p.DataCriacao, Status = p.Status,
                    TipoAtendimento = p.TipoAtendimento, Total = p.Total, FormaPagamento = p.NomeFormaPagamentoSnapshot,
                    QuantidadeItens = p.Itens.Sum(i => i.Quantidade), Pago = p.Pago,
                    // A projeção usa a comanda do próprio pedido, já filtrado por EmpresaId na consulta principal.
                    NomeMesa = p.TipoAtendimento == TipoAtendimento.Mesa ? p.Comanda!.Mesa!.Nome : null,
                    NumeroComanda = p.TipoAtendimento == TipoAtendimento.Mesa ? p.Comanda!.NumeroComanda : null,
                    NomeFuncionario = p.TipoAtendimento == TipoAtendimento.Mesa ? p.NomeFuncionarioSnapshot : null
                }).ToListAsync();
            var resumoHoje = await _context.Pedidos.AsNoTracking().Where(p => p.EmpresaId == empresa.Id && p.DataCriacao >= hoje).GroupBy(_ => 1)
                .Select(g => new { Total = g.Count(), Novos = g.Count(p => p.Status == StatusPedido.Novo), Preparo = g.Count(p => p.Status == StatusPedido.EmPreparo), Finalizados = g.Count(p => p.Status == StatusPedido.Entregue) }).FirstOrDefaultAsync();

            // Consolida filtros, cartões e pedidos da mesma empresa para a View.
            // Busca o modo salvo somente para definir a apresentação das colunas da empresa atual.
            var fluxo = await _context.ConfiguracoesLoja.AsNoTracking().Where(c => c.EmpresaId == empresa.Id).Select(c => (TipoFluxoPedido?)c.TipoFluxoPedido).FirstOrDefaultAsync() ?? TipoFluxoPedido.Completo;
            return new PedidosIndexViewModel { Slug = empresa.Slug, Busca = busca, Status = status, DataInicial = dataOperacional,
                // A View recebe o período efetivo para comunicar que, ao limpar datas, ela retorna automaticamente para hoje.
                PeriodoExibicao = dataOperacional == hoje ? $"Hoje - {dataOperacional:dd/MM/yyyy}" : dataOperacional.ToString("dd/MM/yyyy"),
                TipoFluxoPedido = fluxo,
                TotalHoje = resumoHoje?.Total ?? 0, TotalNovos = resumoHoje?.Novos ?? 0, TotalEmPreparo = resumoHoje?.Preparo ?? 0, TotalFinalizados = resumoHoje?.Finalizados ?? 0, Pedidos = pedidos };
        }

        private async Task<PedidoManualCreateViewModel> CriarPedidoManualAsync(Empresa empresa)
        {
            // Carrega categorias ativas do tenant para a composição do pedido manual.
            var categorias = await _context.Categorias.AsNoTracking().Where(c => c.EmpresaId == empresa.Id && c.Ativa)
                .OrderBy(c => c.OrdemExibicao).ThenBy(c => c.Nome)
                .Select(c => new PedidoManualCategoriaViewModel { Id = c.Id, Nome = c.Nome }).ToListAsync();
            // Reúne as categorias permitidas para limitar produtos e adicionais ao mesmo tenant.
            var categoriaIds = categorias.Select(c => c.Id).ToList();
            var produtos = await _context.Produtos.AsNoTracking().Where(p => p.EmpresaId == empresa.Id && p.Ativo && categoriaIds.Contains(p.CategoriaId))
                .OrderBy(p => p.OrdemExibicao).ThenBy(p => p.Nome)
                .Select(p => new PedidoManualProdutoViewModel { Id = p.Id, CategoriaId = p.CategoriaId, Nome = p.Nome, Preco = p.Preco, PrecoPromocional = p.PrecoPromocional, PermiteObservacao = p.PermiteObservacao }).ToListAsync();
            var adicionais = await _context.AdicionalCategorias.AsNoTracking()
                .Where(ac => categoriaIds.Contains(ac.CategoriaId) && ac.Adicional!.EmpresaId == empresa.Id && ac.Adicional.Ativo)
                .OrderBy(ac => ac.Adicional!.Nome)
                .Select(ac => new PedidoManualAdicionalViewModel { Id = ac.AdicionalId, CategoriaId = ac.CategoriaId, Nome = ac.Adicional!.Nome, Preco = ac.Adicional.Preco }).ToListAsync();
            var bairros = await _context.BairrosEntrega.AsNoTracking().Where(b => b.EmpresaId == empresa.Id && b.Ativo)
                .OrderBy(b => b.OrdemExibicao).ThenBy(b => b.NomeBairro)
                .Select(b => new PedidoManualBairroViewModel { Id = b.Id, Nome = b.NomeBairro, TaxaEntrega = b.TaxaEntrega, PedidoMinimo = b.PedidoMinimo }).ToListAsync();
            var pagamentos = await _context.FormasPagamento.AsNoTracking().Where(f => f.EmpresaId == empresa.Id && f.Ativa)
                .OrderBy(f => f.OrdemExibicao).ThenBy(f => f.Nome)
                .Select(f => new PedidoManualFormaPagamentoViewModel { Id = f.Id, Nome = f.Nome, Tipo = f.Tipo, AceitaTroco = f.AceitaTroco }).ToListAsync();
            // Compõe o ViewModel sem expor itens de outra empresa.
            return new PedidoManualCreateViewModel { Slug = empresa.Slug, Categorias = categorias, Produtos = produtos, Adicionais = adicionais, Bairros = bairros, FormasPagamento = pagamentos };
        }
    }
}
