using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Enums;
using PedZapp.Helpers;
using PedZapp.Models;
using PedZapp.ViewModels.Pedido;

namespace PedZapp.Services
{
    /// <summary>
    /// Centraliza a criação segura de pedidos para checkout público e pedido manual.
    /// O cliente envia apenas escolhas; preços, taxa, total, disponibilidade e vínculos com a empresa
    /// são sempre recalculados no servidor antes da transação ser persistida.
    /// </summary>
    public sealed class PedidoService : IPedidoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IStatusLojaService _statusLoja;
        private readonly IPedidoNotificacaoService _notificacoes;
        private readonly ILogger<PedidoService> _logger;

        public PedidoService(
            ApplicationDbContext context,
            IStatusLojaService statusLoja,
            IPedidoNotificacaoService notificacoes,
            ILogger<PedidoService> logger)
        {
            _context = context;
            _statusLoja = statusLoja;
            _notificacoes = notificacoes;
            _logger = logger;
        }

        public async Task<FinalizacaoPedidoResultado> CriarAsync(string slug, FinalizarPedidoRequestVM request, OrigemPedido origem = OrigemPedido.Site)
        {
            if (!Guid.TryParse(request.ChaveIdempotencia, out var chaveIdempotencia))
                return FinalizacaoPedidoResultado.Invalido("Não foi possível validar esta solicitação. Atualize a página e tente novamente.");

            var empresa = await _context.Empresas.AsNoTracking()
                .Where(e => e.Slug == slug)
                .Select(e => new EmpresaPedidoConsulta
                {
                    Id = e.Id,
                    Slug = e.Slug,
                    Ativa = e.Ativa,
                    CardapioPublicado = e.ConfiguracaoLoja != null && e.ConfiguracaoLoja.CardapioPublicado,
                    AceitaPedidos = e.ConfiguracaoLoja != null && e.ConfiguracaoLoja.AceitaPedidos
                })
                .FirstOrDefaultAsync();

            if (empresa is null) return FinalizacaoPedidoResultado.NaoEncontrado();
            // Reutiliza a mesma decisão usada pelo painel e cardápio antes de aceitar um novo pedido público.
            var statusLoja = await _statusLoja.ObterStatusAsync(empresa.Id);
            if (!statusLoja.Aberta)
                return FinalizacaoPedidoResultado.Invalido(statusLoja.Mensagem);

            var erroBasico = ValidarBasico(request);
            if (erroBasico is not null) return FinalizacaoPedidoResultado.Invalido(erroBasico);

            var existente = await _context.Pedidos.AsNoTracking()
                .Where(p => p.EmpresaId == empresa.Id && p.ChaveIdempotencia == chaveIdempotencia)
                .Select(p => p.CodigoPublico).FirstOrDefaultAsync();
            if (existente is not null) return FinalizacaoPedidoResultado.Criado(existente);

            var produtoIds = request.Itens.Select(i => i.ProdutoId).Distinct().ToList();
            var produtos = await _context.Produtos.AsNoTracking()
                .Where(p => p.EmpresaId == empresa.Id && p.Ativo && p.Categoria!.Ativa && produtoIds.Contains(p.Id))
                .Select(p => new ProdutoPedidoConsulta
                {
                    Id = p.Id,
                    CategoriaId = p.CategoriaId,
                    Nome = p.Nome,
                    Disponivel = p.Disponivel,
                    Preco = p.Preco,
                    PrecoPromocional = p.PrecoPromocional
                }).ToListAsync();
            if (produtos.Count != produtoIds.Count)
                return FinalizacaoPedidoResultado.Invalido("Um dos produtos não está mais disponível.");
            var nomesIndisponiveis = produtos.Where(p => !p.Disponivel).Select(p => p.Nome).ToList();
            if (nomesIndisponiveis.Count > 0)
                return FinalizacaoPedidoResultado.Invalido($"Alguns itens do seu carrinho ficaram indisponíveis: {string.Join(", ", nomesIndisponiveis)}.");

            var adicionalIds = request.Itens.SelectMany(i => i.AdicionalIds).Distinct().ToList();
            var adicionais = await _context.Adicionais.AsNoTracking()
                .Where(a => a.EmpresaId == empresa.Id && a.Ativo && adicionalIds.Contains(a.Id))
                .Select(a => new AdicionalPedidoConsulta { Id = a.Id, Nome = a.Nome, Preco = a.Preco, MaximoSelecao = a.MaximoSelecao })
                .ToListAsync();
            if (adicionais.Count != adicionalIds.Count)
                return FinalizacaoPedidoResultado.Invalido("Um dos adicionais não está mais disponível.");

            var categoriaIds = produtos.Select(p => p.CategoriaId).Distinct().ToList();
            var vinculos = await _context.AdicionalCategorias.AsNoTracking()
                .Where(ac => categoriaIds.Contains(ac.CategoriaId) && adicionalIds.Contains(ac.AdicionalId))
                .Select(ac => new { ac.CategoriaId, ac.AdicionalId }).ToListAsync();

            var bairro = await ObterBairroAsync(empresa.Id, request);
            if (bairro.Erro is not null) return FinalizacaoPedidoResultado.Invalido(bairro.Erro);

            var pagamento = await _context.FormasPagamento.AsNoTracking()
                .Where(f => f.Id == request.FormaPagamentoId && f.EmpresaId == empresa.Id && f.Ativa)
                .Select(f => new FormaPagamentoPedidoConsulta { Id = f.Id, Nome = f.Nome, Tipo = f.Tipo, AceitaTroco = f.AceitaTroco })
                .FirstOrDefaultAsync();
            if (pagamento is null) return FinalizacaoPedidoResultado.Invalido("Selecione uma forma de pagamento disponível.");

            // Valores exibidos no carrinho nunca são confiados: a base de preços vem exclusivamente destas consultas filtradas pelo tenant.
            var produtosPorId = produtos.ToDictionary(p => p.Id);
            var adicionaisPorId = adicionais.ToDictionary(a => a.Id);
            var pedido = new Pedido
            {
                EmpresaId = empresa.Id,
                NumeroPedido = GerarNumeroPedido(),
                CodigoPublico = Guid.NewGuid().ToString("N"),
                ChaveIdempotencia = chaveIdempotencia,
                Origem = origem,
                Status = StatusPedido.Novo,
                TipoAtendimento = request.TipoAtendimento,
                NomeCliente = Limitar(request.NomeCliente, 160),
                TelefoneCliente = Limitar(request.TelefoneCliente, 20),
                // Apenas pedidos públicos carregam o consentimento informado pelo próprio cliente no checkout.
                AceitaAtualizacoesWhatsApp = origem == OrigemPedido.Site && request.AceitaAtualizacoesWhatsApp,
                BairroEntregaId = bairro.Bairro?.Id,
                NomeBairroSnapshot = bairro.Bairro?.Nome,
                TaxaEntrega = bairro.Taxa,
                Rua = request.TipoAtendimento == TipoAtendimento.Entrega ? LimitarOuNulo(request.Rua, 160) : null,
                NumeroEndereco = request.TipoAtendimento == TipoAtendimento.Entrega ? (request.SemNumero ? "Sem número" : LimitarOuNulo(request.NumeroEndereco, 20)) : null,
                Complemento = request.TipoAtendimento == TipoAtendimento.Entrega ? LimitarOuNulo(request.Complemento, 160) : null,
                Referencia = request.TipoAtendimento == TipoAtendimento.Entrega ? LimitarOuNulo(request.Referencia, 300) : null,
                FormaPagamentoId = pagamento.Id,
                NomeFormaPagamentoSnapshot = pagamento.Nome,
                PrecisaTroco = request.PrecisaTroco,
                ObservacaoGeral = LimitarOuNulo(request.ObservacaoGeral, 500)
            };

            foreach (var itemRequest in request.Itens)
            {
                var produto = produtosPorId[itemRequest.ProdutoId];
                var idsAdicionaisItem = itemRequest.AdicionalIds.Distinct().ToList();
                var adicionaisItem = idsAdicionaisItem.Select(id => adicionaisPorId[id]).ToList();
                if (adicionaisItem.Any(a => !vinculos.Any(v => v.CategoriaId == produto.CategoriaId && v.AdicionalId == a.Id)))
                    return FinalizacaoPedidoResultado.Invalido("Um adicional não pertence ao produto selecionado.");
                if (adicionaisItem.Any(a => a.MaximoSelecao is > 0) && idsAdicionaisItem.Count > adicionaisItem.Where(a => a.MaximoSelecao is > 0).Min(a => a.MaximoSelecao!.Value))
                    return FinalizacaoPedidoResultado.Invalido("A quantidade máxima de adicionais foi excedida.");

                var precoProduto = produto.PrecoPromocional.HasValue
                    && produto.PrecoPromocional.Value >= 0
                    && produto.PrecoPromocional.Value <= produto.Preco
                    ? produto.PrecoPromocional.Value
                    : produto.Preco;
                if (precoProduto < 0 || adicionaisItem.Any(a => a.Preco < 0))
                    return FinalizacaoPedidoResultado.Invalido("Não foi possível calcular o pedido.");
                var precoAdicionais = adicionaisItem.Sum(a => a.Preco);
                var item = new PedidoItem
                {
                    ProdutoId = produto.Id,
                    NomeProdutoSnapshot = produto.Nome,
                    PrecoUnitario = precoProduto,
                    Quantidade = itemRequest.Quantidade,
                    Observacao = LimitarOuNulo(itemRequest.Observacao, 500),
                    Subtotal = (precoProduto + precoAdicionais) * itemRequest.Quantidade
                };
                foreach (var adicional in adicionaisItem)
                    item.Adicionais.Add(new PedidoItemAdicional { AdicionalId = adicional.Id, NomeAdicionalSnapshot = adicional.Nome, PrecoUnitario = adicional.Preco, Quantidade = 1 });
                pedido.Itens.Add(item);
            }

            pedido.Subtotal = pedido.Itens.Sum(i => i.Subtotal);
            pedido.Total = pedido.Subtotal + pedido.TaxaEntrega;
            if (pedido.Total < 0) return FinalizacaoPedidoResultado.Invalido("Não foi possível calcular o pedido.");
            if (bairro.Bairro?.PedidoMinimo is decimal minimo && pedido.Subtotal < minimo)
                return FinalizacaoPedidoResultado.Invalido("O pedido mínimo para este bairro não foi atingido.");

            var erroTroco = ValidarTroco(request, pagamento, pedido.Total);
            if (erroTroco is not null) return FinalizacaoPedidoResultado.Invalido(erroTroco);
            pedido.TrocoPara = request.PrecisaTroco ? ParseDecimal(request.TrocoPara) : null;

            try
            {
                // Pedido, itens e adicionais são gravados como uma única unidade para evitar vendas parciais.
                await using var transacao = await _context.Database.BeginTransactionAsync();
                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();
                await transacao.CommitAsync();

                // A venda já está confirmada no banco. Somente pedidos externos geram aviso para não alertar a equipe sobre sua própria ação manual.
                _logger.LogInformation(
                    "Pedido público {PedidoId} criado com sucesso para a empresa {EmpresaId}.",
                    pedido.Id,
                    pedido.EmpresaId);
                if (origem == OrigemPedido.Site)
                    await _notificacoes.NotificarNovoPedidoAsync(pedido, empresa.Slug);

                return FinalizacaoPedidoResultado.Criado(pedido.CodigoPublico);
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                var pedidoDuplicado = await _context.Pedidos.AsNoTracking()
                    .Where(p => p.EmpresaId == empresa.Id && p.ChaveIdempotencia == chaveIdempotencia)
                    .Select(p => p.CodigoPublico).FirstOrDefaultAsync();
                return pedidoDuplicado is not null
                    ? FinalizacaoPedidoResultado.Criado(pedidoDuplicado)
                    : FinalizacaoPedidoResultado.Invalido("Não foi possível registrar o pedido. Tente novamente.");
            }
        }

        private async Task<(BairroPedidoConsulta? Bairro, decimal Taxa, string? Erro)> ObterBairroAsync(int empresaId, FinalizarPedidoRequestVM request)
        {
            if (request.TipoAtendimento == TipoAtendimento.Retirada) return (null, 0, null);
            // Bairro e número (quando aplicável) continuam obrigatórios; Rua pode ser omitida para locais sem endereço convencional.
            if (request.BairroEntregaId is not int bairroId || (!request.SemNumero && string.IsNullOrWhiteSpace(request.NumeroEndereco)))
                return (null, 0, "Informe corretamente o endereço de entrega.");
            var bairro = await _context.BairrosEntrega.AsNoTracking().Where(b => b.Id == bairroId && b.EmpresaId == empresaId && b.Ativo)
                .Select(b => new BairroPedidoConsulta { Id = b.Id, Nome = b.NomeBairro, Taxa = b.TaxaEntrega, PedidoMinimo = b.PedidoMinimo }).FirstOrDefaultAsync();
            return bairro is null ? (null, 0, "Selecione um bairro de entrega disponível.") : (bairro, bairro.Taxa, null);
        }

        private static string? ValidarBasico(FinalizarPedidoRequestVM request)
        {
            if (!Enum.IsDefined(request.TipoAtendimento)) return "Selecione o tipo de atendimento.";
            if (request.Itens.Count is < 1 or > 100) return "Informe ao menos um item válido.";
            if (request.Itens.Any(i => i.ProdutoId <= 0 || i.Quantidade is < 1 or > 99 || i.AdicionalIds.Count > 30 || i.Observacao?.Length > 500)) return "Um dos itens informados é inválido.";
            if (string.IsNullOrWhiteSpace(request.NomeCliente) || request.NomeCliente.Length > 160) return "Informe seu nome.";
            if (request.TelefoneCliente.Count(char.IsDigit) is < 10 or > 11) return "Informe um telefone válido.";
            if (request.FormaPagamentoId <= 0) return "Selecione uma forma de pagamento.";
            return null;
        }

        private static string? ValidarTroco(FinalizarPedidoRequestVM request, FormaPagamentoPedidoConsulta pagamento, decimal total)
        {
            if (!request.PrecisaTroco) return null;
            if (pagamento.Tipo != TipoFormaPagamento.Dinheiro || !pagamento.AceitaTroco) return "Troco está disponível apenas para dinheiro.";
            var valor = ParseDecimal(request.TrocoPara);
            return valor is null || valor < total ? "O valor para troco deve ser igual ou maior que o total do pedido." : null;
        }

        // Reutiliza a regra de entrada monetária do MVC para que 4,00 nunca seja interpretado como 400 ao calcular troco.
        private static decimal? ParseDecimal(string? valor) => DecimalPtBrInputParser.TryParse(valor, out var numero) ? numero : null;
        // O GUID completo evita a colisão que ocorreria ao truncar seu sufixo durante vários checkouts simultâneos.
        // O índice único (EmpresaId, NumeroPedido) continua sendo a proteção final no banco.
        private static string GerarNumeroPedido() => $"{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}".ToUpperInvariant();
        private static string Limitar(string valor, int tamanho) => valor.Trim()[..Math.Min(valor.Trim().Length, tamanho)];
        private static string? LimitarOuNulo(string? valor, int tamanho) => string.IsNullOrWhiteSpace(valor) ? null : Limitar(valor, tamanho);

        private sealed class EmpresaPedidoConsulta { public int Id { get; init; } public string Slug { get; init; } = string.Empty; public bool Ativa { get; init; } public bool CardapioPublicado { get; init; } public bool AceitaPedidos { get; init; } }
        private sealed class ProdutoPedidoConsulta { public int Id { get; init; } public int CategoriaId { get; init; } public string Nome { get; init; } = string.Empty; public bool Disponivel { get; init; } public decimal Preco { get; init; } public decimal? PrecoPromocional { get; init; } }
        private sealed class AdicionalPedidoConsulta { public int Id { get; init; } public string Nome { get; init; } = string.Empty; public decimal Preco { get; init; } public int? MaximoSelecao { get; init; } }
        private sealed class BairroPedidoConsulta { public int Id { get; init; } public string Nome { get; init; } = string.Empty; public decimal Taxa { get; init; } public decimal? PedidoMinimo { get; init; } }
        private sealed class FormaPagamentoPedidoConsulta { public int Id { get; init; } public string Nome { get; init; } = string.Empty; public TipoFormaPagamento Tipo { get; init; } public bool AceitaTroco { get; init; } }
    }

    public sealed class FinalizacaoPedidoResultado
    {
        public bool Sucesso { get; private init; }
        public bool SlugNaoEncontrado { get; private init; }
        public string? CodigoPublico { get; private init; }
        public string? Erro { get; private init; }
        public static FinalizacaoPedidoResultado Criado(string codigo) => new() { Sucesso = true, CodigoPublico = codigo };
        public static FinalizacaoPedidoResultado NaoEncontrado() => new() { SlugNaoEncontrado = true };
        public static FinalizacaoPedidoResultado Invalido(string erro) => new() { Erro = erro };
    }
}
