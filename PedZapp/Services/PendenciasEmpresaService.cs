using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.ViewModels.PainelEmpresa;

namespace PedZapp.Services;

/// <summary>
/// Gera pendências a partir de registros existentes do tenant, sem alterar configurações,
/// pedidos ou catálogo. Cada consulta é sempre limitada pelo EmpresaId recebido do servidor.
/// </summary>
public sealed class PendenciasEmpresaService : IPendenciasEmpresaService
{
    private readonly ApplicationDbContext _context;

    public PendenciasEmpresaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PendenciasEmpresaResultadoViewModel> ObterPendenciasAsync(int empresaId, string slug)
    {
        // Consulta logo e vencimento sem carregar bytes binários, preservando a fronteira do tenant.
        var empresa = await _context.Empresas.AsNoTracking()
            .Where(e => e.Id == empresaId)
            .Select(e => new { PossuiLogo = e.LogoDados != null || e.Logo != null, e.DataExpiracaoPlano })
            .FirstOrDefaultAsync();

        if (empresa is null)
            return new PendenciasEmpresaResultadoViewModel();

        // Lê somente a publicação do cardápio da empresa atual; uma configuração ausente também deixa o cardápio indisponível.
        var configuracao = await _context.ConfiguracoesLoja.AsNoTracking()
            .Where(c => c.EmpresaId == empresaId)
            .Select(c => new { c.CardapioPublicado })
            .FirstOrDefaultAsync();

        // Verifica produtos ativos sem materializar imagens ou entidades completas.
        var produtosSemImagem = await _context.Produtos.AsNoTracking()
            .CountAsync(p => p.EmpresaId == empresaId && p.Ativo && p.ImagemProduto == null);

        // Pagamentos, horários e bairros são verificações independentes e leves, sempre no mesmo tenant.
        var possuiPagamentoAtivo = await _context.FormasPagamento.AsNoTracking()
            .AnyAsync(f => f.EmpresaId == empresaId && f.Ativa);
        var possuiHorarioValido = await _context.HorariosFuncionamento.AsNoTracking()
            .AnyAsync(h => h.EmpresaId == empresaId && h.Ativo && !h.Fechado && h.Abertura1.HasValue && h.Fechamento1.HasValue);
        var possuiBairroAtivo = await _context.BairrosEntrega.AsNoTracking()
            .AnyAsync(b => b.EmpresaId == empresaId && b.Ativo);

        var pendencias = new List<PendenciaEmpresaViewModel>();
        var baseUrl = "/" + Uri.EscapeDataString(slug);

        if (configuracao is null || !configuracao.CardapioPublicado)
            pendencias.Add(Criar("Cardápio não publicado", "Seu cardápio não está disponível para os clientes.", baseUrl + "/cardapio", "Gerenciar cardápio", PrioridadePendenciaEmpresa.Alta));

        if (!possuiPagamentoAtivo)
            pendencias.Add(Criar("Formas de pagamento", "Nenhuma forma de pagamento está ativa.", baseUrl + "/formas-pagamento", "Configurar pagamentos", PrioridadePendenciaEmpresa.Alta));

        // O checkout público atual oferece entrega; por isso, a falta de bairros ativos impede esse atendimento real.
        if (!possuiBairroAtivo)
            pendencias.Add(Criar("Taxas de entrega", "Nenhum bairro de entrega está configurado.", baseUrl + "/entregas", "Configurar taxas", PrioridadePendenciaEmpresa.Alta));

        if (!possuiHorarioValido)
            pendencias.Add(Criar("Horários não configurados", "Configure os horários de funcionamento da loja.", baseUrl + "/horarios", "Configurar horários", PrioridadePendenciaEmpresa.Media));

        if (produtosSemImagem > 0)
            pendencias.Add(Criar("Produtos sem imagem", $"{produtosSemImagem} produto{(produtosSemImagem == 1 ? string.Empty : "s")} ativo{(produtosSemImagem == 1 ? string.Empty : "s")} ainda {(produtosSemImagem == 1 ? "está" : "estão")} sem imagem.", baseUrl + "/produtos", "Ver produtos", PrioridadePendenciaEmpresa.Media));

        if (!empresa.PossuiLogo)
            pendencias.Add(Criar("Adicione sua logo", "Sua empresa ainda não possui uma logo cadastrada.", baseUrl + "/configuracoes", "Configurar identidade", PrioridadePendenciaEmpresa.Baixa));

        // O vencimento só é comunicado quando há uma data real salva; nenhuma assinatura fictícia é criada.
        var hoje = DateTime.Today;
        if (empresa.DataExpiracaoPlano.HasValue && empresa.DataExpiracaoPlano.Value.Date <= hoje.AddDays(7))
        {
            var vencido = empresa.DataExpiracaoPlano.Value.Date < hoje;
            pendencias.Add(Criar(vencido ? "Plano vencido" : "Plano próximo do vencimento",
                vencido ? "Verifique as configurações do seu plano." : $"Seu plano vence em {empresa.DataExpiracaoPlano.Value:dd/MM/yyyy}.",
                baseUrl + "/configuracoes", "Ver configurações", PrioridadePendenciaEmpresa.Alta));
        }

        var ordenadas = pendencias.OrderBy(p => p.Prioridade).ThenBy(p => p.Titulo).ToList();
        const int limiteDashboard = 4;
        return new PendenciasEmpresaResultadoViewModel
        {
            Total = ordenadas.Count,
            OutrasPendencias = Math.Max(0, ordenadas.Count - limiteDashboard),
            Pendencias = ordenadas.Take(limiteDashboard).ToList()
        };
    }

    // Cria um item de apresentação sem expor EmpresaId ou entidades para o Razor.
    private static PendenciaEmpresaViewModel Criar(string titulo, string descricao, string url, string textoBotao, PrioridadePendenciaEmpresa prioridade) =>
        new() { Titulo = titulo, Descricao = descricao, UrlResolucao = url, TextoBotao = textoBotao, Prioridade = prioridade };
}
