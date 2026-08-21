using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Enums;
using PedZapp.Models;
using PedZapp.ViewModels.Pedido;

namespace PedZapp.Services
{
    /// <summary>
    /// Implementação inicial da fila de impressão que prepara uma via para o navegador.
    /// Não confirma saída física de papel; a mesma interface permite substituir este serviço por
    /// um agente local ou implementação ESC/POS futuramente.
    /// </summary>
    public sealed class BrowserPedidoPrintService : IPedidoPrintService
    {
        private const string ChaveConfirmacaoInicial = "confirmacao-inicial";
        private readonly ApplicationDbContext _context;
        public BrowserPedidoPrintService(ApplicationDbContext context) => _context = context;

        public Task<SolicitacaoImpressaoResultado> CriarParaConfirmacaoAsync(int pedidoId, int empresaId) => CriarAsync(pedidoId, empresaId, TipoImpressao.Cozinha, ChaveConfirmacaoInicial);
        public Task<SolicitacaoImpressaoResultado> CriarReimpressaoAsync(int pedidoId, int empresaId, TipoImpressao tipo) => CriarAsync(pedidoId, empresaId, tipo, $"reimpressao-{Guid.NewGuid():N}");

        public async Task<PedidoImpressaoViewModel?> ObterParaImpressaoAsync(int empresaId, string codigoPublico, string tokenPublico)
        {
            var impressao = await _context.ImpressaoPedidos.FirstOrDefaultAsync(i => i.EmpresaId == empresaId && i.TokenPublico == tokenPublico && i.Pedido!.CodigoPublico == codigoPublico && i.Ativa);
            if (impressao is null) return null;
            if (impressao.StatusImpressao == StatusImpressao.Pendente)
            {
                impressao.StatusImpressao = StatusImpressao.Processando;
                await _context.SaveChangesAsync();
            }

            return await _context.ImpressaoPedidos.AsNoTracking().Where(i => i.Id == impressao.Id && i.EmpresaId == empresaId)
                .Select(i => new PedidoImpressaoViewModel
                {
                    Slug = i.Empresa!.Slug, CodigoPublico = i.Pedido!.CodigoPublico, TokenImpressao = i.TokenPublico,
                    NomeEmpresa = i.Empresa.NomeFantasia, NumeroPedido = i.Pedido.NumeroPedido, Mesa = i.Pedido.Comanda != null ? i.Pedido.Comanda.Mesa!.Nome : null, NumeroComanda = i.Pedido.Comanda != null ? i.Pedido.Comanda.NumeroComanda : null, Funcionario = i.Pedido.NomeFuncionarioSnapshot, DataCriacao = i.Pedido.DataCriacao,
                    Origem = i.Pedido.Origem, TipoImpressao = i.TipoImpressao, TipoAtendimento = i.Pedido.TipoAtendimento,
                    NomeCliente = i.Pedido.NomeCliente, TelefoneCliente = i.Pedido.TelefoneCliente, Bairro = i.Pedido.NomeBairroSnapshot,
                    Rua = i.Pedido.Rua, NumeroEndereco = i.Pedido.NumeroEndereco, Complemento = i.Pedido.Complemento, Referencia = i.Pedido.Referencia,
                    FormaPagamento = i.Pedido.NomeFormaPagamentoSnapshot, PrecisaTroco = i.Pedido.PrecisaTroco, TrocoPara = i.Pedido.TrocoPara,
                    Subtotal = i.Pedido.Subtotal, TaxaEntrega = i.Pedido.TaxaEntrega, Total = i.Pedido.Total, ObservacaoGeral = i.Pedido.ObservacaoGeral,
                    Itens = i.Pedido.Itens.Select(item => new PedidoImpressaoItemViewModel { Nome = item.NomeProdutoSnapshot, Quantidade = item.Quantidade, /* A impressão usa os snapshots financeiros do item, e não o preço atual do produto. */ PrecoUnitario = item.PrecoUnitario, Subtotal = item.Subtotal, Observacao = item.Observacao, Adicionais = item.Adicionais.Select(adicional => adicional.NomeAdicionalSnapshot).ToList() }).ToList()
                }).FirstOrDefaultAsync();
        }

        public async Task<bool> RegistrarTentativaAsync(int empresaId, string codigoPublico, string tokenPublico)
        {
            var impressao = await _context.ImpressaoPedidos.FirstOrDefaultAsync(i => i.EmpresaId == empresaId && i.TokenPublico == tokenPublico && i.Pedido!.CodigoPublico == codigoPublico && i.Ativa);
            if (impressao is null) return false;
            if (impressao.StatusImpressao != StatusImpressao.Solicitada)
            {
                impressao.StatusImpressao = StatusImpressao.Solicitada;
                impressao.QuantidadeTentativas++;
                impressao.DataImpressao = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return true;
        }

        private async Task<SolicitacaoImpressaoResultado> CriarAsync(int pedidoId, int empresaId, TipoImpressao tipo, string chaveEvento)
        {
            // Pedido e solicitação são buscados sempre pelo tenant recebido pelo controller autenticado.
            var pedido = await _context.Pedidos.AsNoTracking().Where(p => p.Id == pedidoId && p.EmpresaId == empresaId).Select(p => new { p.Id, p.EmpresaId, p.CodigoPublico }).FirstOrDefaultAsync();
            if (pedido is null) return SolicitacaoImpressaoResultado.NaoEncontrado();

            var existente = await _context.ImpressaoPedidos.AsNoTracking().Where(i => i.PedidoId == pedidoId && i.TipoImpressao == tipo && i.ChaveEvento == chaveEvento).Select(i => i.TokenPublico).FirstOrDefaultAsync();
            if (existente is not null) return SolicitacaoImpressaoResultado.Criada(pedido.CodigoPublico, existente);

            var impressao = new ImpressaoPedido { EmpresaId = pedido.EmpresaId, PedidoId = pedido.Id, TipoImpressao = tipo, ChaveEvento = chaveEvento };
            try
            {
                _context.ImpressaoPedidos.Add(impressao);
                await _context.SaveChangesAsync();
                return SolicitacaoImpressaoResultado.Criada(pedido.CodigoPublico, impressao.TokenPublico);
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                var concorrente = await _context.ImpressaoPedidos.AsNoTracking().Where(i => i.PedidoId == pedidoId && i.TipoImpressao == tipo && i.ChaveEvento == chaveEvento).Select(i => i.TokenPublico).FirstOrDefaultAsync();
                return concorrente is null ? SolicitacaoImpressaoResultado.Falha("Não foi possível registrar a impressão.") : SolicitacaoImpressaoResultado.Criada(pedido.CodigoPublico, concorrente);
            }
        }
    }
}
