using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Enums;
using PedZapp.Models;
using PedZapp.ViewModels.Mesa;

namespace PedZapp.Services
{
    /// <summary>Orquestra abertura, consumo, envio e fechamento de comandas sem aceitar valores financeiros do navegador.</summary>
    public sealed class ComandaService : IComandaService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ComandaService> _logger;
        public ComandaService(ApplicationDbContext context, ILogger<ComandaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(bool Sucesso, string? Erro)> AbrirAsync(int mesaId, int empresaId, ApplicationUser usuario)
        {
            var mesa = await _context.Mesas.FirstOrDefaultAsync(m => m.Id == mesaId && m.EmpresaId == empresaId);
            if (mesa is null || !mesa.Ativa || mesa.Status != StatusMesa.Livre) return (false, "Esta mesa não está disponível.");
            await using var tx = await _context.Database.BeginTransactionAsync();
            if (await _context.Comandas.AnyAsync(c => c.MesaId == mesaId && c.Ativa)) return (false, "Já existe uma comanda aberta para esta mesa.");
            var nome = string.IsNullOrWhiteSpace(usuario.UserName) ? usuario.Email ?? "Funcionário" : usuario.UserName;
            _context.Comandas.Add(new Comanda { EmpresaId = empresaId, MesaId = mesaId, NumeroComanda = $"C{DateTime.UtcNow:yyyyMMddHHmm}-{mesaId}", CriadaPorUsuarioId = usuario.Id, NomeFuncionarioSnapshot = nome, Total = 0 });
            mesa.Status = StatusMesa.Ocupada; mesa.DataAtualizacao = DateTime.UtcNow;
            await _context.SaveChangesAsync(); await tx.CommitAsync(); return (true, null);
        }

        public async Task<ComandaViewModel?> ObterAsync(int mesaId, int empresaId, string slug)
        {
            var comanda = await _context.Comandas.AsNoTracking().Where(c => c.MesaId == mesaId && c.EmpresaId == empresaId && c.Ativa)
                .Select(c => new ComandaViewModel { Slug = slug, NomeEmpresa = c.Empresa!.NomeFantasia, MesaId = c.MesaId, NomeMesa = c.Mesa!.Nome, NumeroComanda = c.NumeroComanda, Status = c.Status, DataAbertura = c.DataAbertura, Funcionario = c.NomeFuncionarioSnapshot, Subtotal = c.Subtotal, PercentualTaxaServico = c.PercentualTaxaServico, ValorTaxaServico = c.ValorTaxaServico, TaxaServicoAplicada = c.TaxaServicoAplicada, Total = c.Total, FormaPagamento = c.NomeFormaPagamentoSnapshot, PrecisaTroco = c.PrecisaTroco, TrocoPara = c.TrocoPara,
                    Itens = c.Itens.OrderBy(i => i.Id).Select(i => new ComandaItemViewModel { Id = i.Id, Nome = i.NomeProdutoSnapshot, Quantidade = i.Quantidade, PrecoUnitario = i.PrecoUnitario, Subtotal = i.Subtotal, Observacao = i.Observacao, EnviadoParaCozinha = i.EnviadoParaCozinha, Adicionais = i.Adicionais.Select(a => a.NomeAdicionalSnapshot).ToList() }).ToList() }).FirstOrDefaultAsync();
            if (comanda is null) return null;
            // Produtos temporariamente indisponíveis não entram no catálogo de novos itens da comanda.
            comanda.Produtos = await _context.Produtos.AsNoTracking().Where(p => p.EmpresaId == empresaId && p.Ativo && p.Disponivel && p.Categoria!.Ativa).OrderBy(p => p.Nome).Select(p => new ComandaCatalogoProdutoViewModel { Id = p.Id, CategoriaId = p.CategoriaId, Categoria = p.Categoria!.Nome, Nome = p.Nome, Preco = p.Preco, PrecoPromocional = p.PrecoPromocional }).ToListAsync();
            var categorias = comanda.Produtos.Select(p => p.CategoriaId).Distinct().ToList();
            comanda.Adicionais = await _context.AdicionalCategorias.AsNoTracking().Where(a => categorias.Contains(a.CategoriaId) && a.Adicional!.EmpresaId == empresaId && a.Adicional.Ativo)
                .GroupBy(a => new { a.AdicionalId, a.CategoriaId })
                .Select(g => new ComandaCatalogoAdicionalViewModel { Id = g.Key.AdicionalId, CategoriaId = g.Key.CategoriaId, Nome = g.Min(a => a.Adicional!.Nome) ?? string.Empty, Preco = g.Min(a => a.Adicional!.Preco) }).ToListAsync();
            comanda.FormasPagamento = await _context.FormasPagamento.AsNoTracking().Where(f => f.EmpresaId == empresaId && f.Ativa).OrderBy(f => f.OrdemExibicao).Select(f => new ComandaFormaPagamentoViewModel { Id = f.Id, Nome = f.Nome, Tipo = f.Tipo, AceitaTroco = f.AceitaTroco }).ToListAsync();
            return comanda;
        }

        public async Task<(bool Sucesso, string? Erro)> AdicionarItemAsync(int mesaId, int empresaId, ComandaItemInputViewModel dados)
        {
            var comanda = await _context.Comandas.Include(c => c.Itens).FirstOrDefaultAsync(c => c.MesaId == mesaId && c.EmpresaId == empresaId && c.Ativa);
            if (comanda is null) return (false, "Comanda não encontrada.");
            if (dados.Quantidade is < 1 or > 99 || dados.AdicionalIds.Count > 30) return (false, "Item inválido.");
            // Confere disponibilidade novamente no banco porque o produto pode ter mudado após o catálogo ser carregado.
            var produto = await _context.Produtos.AsNoTracking().Where(p => p.Id == dados.ProdutoId && p.EmpresaId == empresaId && p.Ativo && p.Disponivel && p.Categoria!.Ativa).Select(p => new { p.Id, p.CategoriaId, p.Nome, p.Preco, p.PrecoPromocional }).FirstOrDefaultAsync();
            if (produto is null) return (false, "Produto indisponível.");
            var ids = dados.AdicionalIds.Distinct().ToList();
            var adicionais = await _context.Adicionais.AsNoTracking().Where(a => a.EmpresaId == empresaId && a.Ativo && ids.Contains(a.Id)).Select(a => new { a.Id, a.Nome, a.Preco }).ToListAsync();
            if (adicionais.Count != ids.Count || await _context.AdicionalCategorias.CountAsync(a => a.CategoriaId == produto.CategoriaId && ids.Contains(a.AdicionalId)) != ids.Count) return (false, "Adicional inválido para este produto.");
            var preco = produto.PrecoPromocional.HasValue && produto.PrecoPromocional.Value >= 0 && produto.PrecoPromocional.Value <= produto.Preco ? produto.PrecoPromocional.Value : produto.Preco;
            var item = new ComandaItem { ProdutoId = produto.Id, NomeProdutoSnapshot = produto.Nome, PrecoUnitario = preco, Quantidade = dados.Quantidade, Observacao = Limpar(dados.Observacao, 500), Subtotal = (preco + adicionais.Sum(a => a.Preco)) * dados.Quantidade };
            foreach (var adicional in adicionais) item.Adicionais.Add(new ComandaItemAdicional { AdicionalId = adicional.Id, NomeAdicionalSnapshot = adicional.Nome, PrecoUnitario = adicional.Preco });
            comanda.Itens.Add(item); Recalcular(comanda); await _context.SaveChangesAsync(); return (true, null);
        }

        public async Task<bool> RemoverItemAsync(int mesaId, int itemId, int empresaId)
        {
            var comanda = await _context.Comandas.Include(c => c.Itens).FirstOrDefaultAsync(c => c.MesaId == mesaId && c.EmpresaId == empresaId && c.Ativa);
            var item = comanda?.Itens.FirstOrDefault(i => i.Id == itemId && !i.EnviadoParaCozinha);
            if (item is null || comanda is null) return false;
            _context.ComandaItens.Remove(item); comanda.Itens.Remove(item); Recalcular(comanda); await _context.SaveChangesAsync(); return true;
        }

        public async Task<bool> AtualizarItemPendenteAsync(int mesaId, int itemId, int empresaId, int quantidade, string? observacao)
        {
            if (quantidade is < 1 or > 99) return false;
            var comanda = await _context.Comandas.Include(c => c.Itens).ThenInclude(i => i.Adicionais)
                .FirstOrDefaultAsync(c => c.MesaId == mesaId && c.EmpresaId == empresaId && c.Ativa);
            var item = comanda?.Itens.FirstOrDefault(i => i.Id == itemId && !i.EnviadoParaCozinha);
            if (comanda is null || item is null) return false;

            // O item já pertence à comanda isolada por empresa; o navegador só pode alterar quantidade e observação.
            item.Quantidade = quantidade;
            item.Observacao = Limpar(observacao, 500);
            item.Subtotal = (item.PrecoUnitario + item.Adicionais.Sum(a => a.PrecoUnitario)) * quantidade;
            Recalcular(comanda);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<EnvioComandaResultado> EnviarParaCozinhaAsync(int mesaId, int empresaId, ApplicationUser usuario)
        {
            _logger.LogInformation("Iniciando envio para cozinha da mesa {MesaId} para a empresa {EmpresaId}", mesaId, empresaId);
            var comanda = await _context.Comandas.Include(c => c.Mesa).Include(c => c.Itens).ThenInclude(i => i.Adicionais).FirstOrDefaultAsync(c => c.MesaId == mesaId && c.EmpresaId == empresaId && c.Ativa);
            if (comanda is null)
            {
                _logger.LogWarning("Comanda aberta não encontrada para mesa {MesaId} e empresa {EmpresaId}", mesaId, empresaId);
                return new(false, "Comanda aberta não encontrada.");
            }
            _logger.LogInformation("Comanda encontrada: {ComandaId}. Itens totais: {TotalItens}", comanda.Id, comanda.Itens.Count);
            _logger.LogInformation("Mesa encontrada: {MesaId}", comanda.MesaId);
            var pendentes = comanda.Itens.Where(i => !i.EnviadoParaCozinha).ToList();
            _logger.LogInformation("Itens pendentes da comanda {ComandaId}: {ItensPendentes}", comanda.Id, pendentes.Count);
            if (!pendentes.Any()) return new(false, "Não há itens pendentes para enviar.");
            try
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                // Cada lote de mesa nasce como Novo, que representa Aguardando confirmação no Kanban e impede impressão prematura.
                // O GUID completo mantém o número do lote presencial seguro mesmo quando várias comandas são enviadas simultaneamente.
                var pedido = new Pedido { EmpresaId = empresaId, ComandaId = comanda.Id, NumeroPedido = $"M{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}".ToUpperInvariant(), CodigoPublico = Guid.NewGuid().ToString("N"), ChaveIdempotencia = Guid.NewGuid(), Origem = OrigemPedido.Mesa, TipoAtendimento = TipoAtendimento.Mesa, Status = StatusPedido.Novo, NomeCliente = comanda.Mesa!.Nome, TelefoneCliente = "0000000000", CriadoPorUsuarioId = usuario.Id, NomeFuncionarioSnapshot = comanda.NomeFuncionarioSnapshot, NomeFormaPagamentoSnapshot = "A definir no fechamento", Subtotal = pendentes.Sum(i => i.Subtotal), Total = pendentes.Sum(i => i.Subtotal) };
                _logger.LogInformation("Criando pedido presencial para a comanda {ComandaId} com {QuantidadeItens} itens e {QuantidadeAdicionais} adicionais", comanda.Id, pendentes.Count, pendentes.Sum(i => i.Adicionais.Count));
                foreach (var item in pendentes) { var pedidoItem = new PedidoItem { ProdutoId = item.ProdutoId, NomeProdutoSnapshot = item.NomeProdutoSnapshot, PrecoUnitario = item.PrecoUnitario, Quantidade = item.Quantidade, Observacao = item.Observacao, Subtotal = item.Subtotal }; foreach (var a in item.Adicionais) pedidoItem.Adicionais.Add(new PedidoItemAdicional { AdicionalId = a.AdicionalId, NomeAdicionalSnapshot = a.NomeAdicionalSnapshot, PrecoUnitario = a.PrecoUnitario }); pedido.Itens.Add(pedidoItem); item.EnviadoParaCozinha = true; }
                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Pedido presencial {PedidoId} criado para a comanda {ComandaId}", pedido.Id, comanda.Id);
                await tx.CommitAsync();
                // A impressão é criada apenas na confirmação administrativa pelo PedidosController, evitando duplicidade na criação do lote.
                return new(true, "Pedido de mesa criado e aguardando confirmação.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar itens da comanda {ComandaId} para a cozinha da empresa {EmpresaId}", comanda.Id, empresaId);
                throw;
            }
        }

        public async Task<ComandaViewModel?> ObterContaFinalAsync(string tokenImpressao, int empresaId, string slug)
        {
            return await _context.Comandas.AsNoTracking()
                .Where(c => c.CodigoPublicoSeguro == tokenImpressao && c.EmpresaId == empresaId && !c.Ativa && c.Status == StatusComanda.Fechada)
                .Select(c => new ComandaViewModel
                {
                    Slug = slug, NomeEmpresa = c.Empresa!.NomeFantasia, MesaId = c.MesaId, NomeMesa = c.Mesa!.Nome,
                    NumeroComanda = c.NumeroComanda, Status = c.Status, DataAbertura = c.DataAbertura,
                    Funcionario = c.NomeFuncionarioSnapshot, Subtotal = c.Subtotal, PercentualTaxaServico = c.PercentualTaxaServico,
                    ValorTaxaServico = c.ValorTaxaServico, TaxaServicoAplicada = c.TaxaServicoAplicada, Total = c.Total,
                    FormaPagamento = c.NomeFormaPagamentoSnapshot, PrecisaTroco = c.PrecisaTroco, TrocoPara = c.TrocoPara,
                    Itens = c.Itens.OrderBy(i => i.Id).Select(i => new ComandaItemViewModel
                    {
                        Id = i.Id, Nome = i.NomeProdutoSnapshot, Quantidade = i.Quantidade, PrecoUnitario = i.PrecoUnitario,
                        Subtotal = i.Subtotal, Observacao = i.Observacao, EnviadoParaCozinha = i.EnviadoParaCozinha,
                        Adicionais = i.Adicionais.Select(a => a.NomeAdicionalSnapshot).ToList()
                    }).ToList()
                }).FirstOrDefaultAsync();
        }

        public async Task<FecharComandaResultado> FecharAsync(int mesaId, int empresaId, FecharComandaInputViewModel dados)
        {
            var comanda = await _context.Comandas.Include(c => c.Mesa).Include(c => c.Itens).FirstOrDefaultAsync(c => c.MesaId == mesaId && c.EmpresaId == empresaId && c.Ativa);
            if (comanda is null || comanda.Mesa is null) return new(false, "Comanda não encontrada.");
            if (!comanda.Itens.Any()) return new(false, "Não é possível fechar uma comanda sem itens.");
            if (comanda.Itens.Any(i => !i.EnviadoParaCozinha)) return new(false, "Envie todos os itens para a cozinha antes de fechar a conta.");
            var pedidos = await _context.Pedidos.Where(p => p.ComandaId == comanda.Id && p.EmpresaId == empresaId && !p.Cancelado && p.Status != StatusPedido.Cancelado).ToListAsync();
            if (pedidos.Any(p => p.Status is not StatusPedido.Pronto and not StatusPedido.Entregue)) return new(false, "Existem pedidos da mesa que ainda não estão prontos.");
            var pagamento = await _context.FormasPagamento.AsNoTracking().FirstOrDefaultAsync(f => f.Id == dados.FormaPagamentoId && f.EmpresaId == empresaId && f.Ativa); if (pagamento is null) return new(false, "Selecione uma forma de pagamento válida.");
            if (dados.PrecisaTroco && (!pagamento.AceitaTroco || !dados.TrocoPara.HasValue)) return new(false, "Informe um valor válido para o troco.");
            await using var tx = await _context.Database.BeginTransactionAsync(); Recalcular(comanda, dados.TaxaServicoAplicada, dados.PercentualTaxaServico); comanda.FormaPagamentoId = pagamento.Id; comanda.NomeFormaPagamentoSnapshot = pagamento.Nome; comanda.PrecisaTroco = dados.PrecisaTroco; comanda.TrocoPara = dados.PrecisaTroco ? dados.TrocoPara : null; comanda.Status = StatusComanda.Fechada; comanda.Ativa = false; comanda.DataFechamento = DateTime.UtcNow; comanda.Mesa.Status = StatusMesa.Livre; comanda.Mesa.DataAtualizacao = DateTime.UtcNow;
            foreach (var pedido in pedidos.Where(p => p.Status == StatusPedido.Pronto)) { pedido.Status = StatusPedido.Entregue; pedido.Pago = true; pedido.DataAtualizacao = DateTime.UtcNow; }
            await _context.SaveChangesAsync(); await tx.CommitAsync(); return new(true, null, comanda.CodigoPublicoSeguro);
        }

        private static void Recalcular(Comanda c, bool? aplicar = null, decimal? percentual = null) { c.Subtotal = c.Itens.Sum(i => i.Subtotal); if (aplicar.HasValue) c.TaxaServicoAplicada = aplicar.Value; if (percentual.HasValue) c.PercentualTaxaServico = Math.Clamp(percentual.Value, 0, 100); c.ValorTaxaServico = c.TaxaServicoAplicada ? Math.Round(c.Subtotal * c.PercentualTaxaServico / 100m, 2) : 0; c.Total = c.Subtotal + c.ValorTaxaServico; }
        private static string? Limpar(string? texto, int max) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim()[..Math.Min(texto.Trim().Length, max)];
    }
}
