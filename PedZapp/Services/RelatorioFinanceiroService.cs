using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Enums;
using PedZapp.ViewModels.Relatorio;

namespace PedZapp.Services;

/// <summary>
/// Consolida dados do dashboard no banco de dados. Todas as consultas começam pelo EmpresaId
/// recebido de um controller que já validou o usuário e o slug.
/// </summary>
public sealed class RelatorioFinanceiroService : IRelatorioFinanceiroService
{
    // Mantém o contexto EF para executar agregações assíncronas no banco.
    private readonly ApplicationDbContext _context;

    // Recebe o contexto configurado pela injeção de dependência.
    public RelatorioFinanceiroService(ApplicationDbContext context) => _context = context;

    /// <summary>Monta hoje, semana e mês a partir de um único instante calculado no servidor.</summary>
    public async Task<DashboardRelatorioViewModel> ObterDashboardAsync(int empresaId, string slug, string nomeEmpresa, DateTime agora)
    {
        // Normaliza o início de hoje para impedir que vendas de outros dias entrem nos cards diários.
        var inicioHoje = agora.Date;
        // Define o início da semana como segunda-feira, conforme a regra de calendário do dashboard.
        var inicioSemana = inicioHoje.AddDays(-((7 + (int)inicioHoje.DayOfWeek - (int)DayOfWeek.Monday) % 7));
        // Define o início do mês corrente sem depender de data recebida pelo navegador.
        var inicioMes = new DateTime(inicioHoje.Year, inicioHoje.Month, 1);
        // Mantém o fim de amanhã como limite semiaberto para incluir todo o dia atual.
        var fimHoje = inicioHoje.AddDays(1);

        // Seleciona somente vendas finalizadas e não canceladas do tenant autorizado.
        var vendasValidas = _context.Pedidos.AsNoTracking().Where(p => p.EmpresaId == empresaId && p.Status == StatusPedido.Entregue && !p.Cancelado);
        // Executa os três resumos de períodos com limites semiabertos e sem carregar pedidos em memória.
        var hoje = await ResumirAsync(vendasValidas, inicioHoje, fimHoje);
        var semana = await ResumirAsync(vendasValidas, inicioSemana, fimHoje);
        var mes = await ResumirAsync(vendasValidas, inicioMes, fimHoje);

        // Conta tipos de atendimento de hoje usando os pedidos financeiramente válidos.
        var atendimentosHoje = await vendasValidas.Where(p => p.DataCriacao >= inicioHoje && p.DataCriacao < fimHoje)
            .GroupBy(p => p.TipoAtendimento)
            .Select(g => new { Tipo = g.Key, Quantidade = g.Count(), Valor = g.Sum(p => p.Total) })
            .ToListAsync();
        // Conta pedidos manuais por sua origem histórica, sem misturá-los com os demais cards.
        var pedidosManuaisHoje = await vendasValidas.CountAsync(p => p.DataCriacao >= inicioHoje && p.DataCriacao < fimHoje && p.Origem == OrigemPedido.Manual);
        // Soma taxas de serviço somente em comandas fechadas hoje, evitando repetição por item de pedido.
        var taxasServicoHoje = await _context.Comandas.AsNoTracking().Where(c => c.EmpresaId == empresaId && c.Status == StatusComanda.Fechada && c.DataFechamento >= inicioHoje && c.DataFechamento < fimHoje && c.TaxaServicoAplicada)
            .Select(c => (decimal?)c.ValorTaxaServico).SumAsync() ?? 0m;
        // Conta cancelamentos como indicador separado, sem incluí-los no faturamento válido.
        var cancelamentosHoje = await _context.Pedidos.AsNoTracking().CountAsync(p => p.EmpresaId == empresaId && p.DataCriacao >= inicioHoje && p.DataCriacao < fimHoje && (p.Cancelado || p.Status == StatusPedido.Cancelado));

        // Agrupa pagamentos pelo snapshot histórico preservado em cada pedido concluído.
        var pagamentosHoje = await vendasValidas.Where(p => p.DataCriacao >= inicioHoje && p.DataCriacao < fimHoje)
            .GroupBy(p => string.IsNullOrEmpty(p.NomeFormaPagamentoSnapshot) ? "Não informado" : p.NomeFormaPagamentoSnapshot)
            .Select(g => new { Rotulo = g.Key, Quantidade = g.Count(), Valor = g.Sum(p => p.Total) }).ToListAsync();
        // Converte os grupos de pagamento em dados seguros para o gráfico de rosca.
        var formasPagamento = pagamentosHoje.Select(p => new FormaPagamentoGraficoViewModel
        {
            Rotulo = p.Rotulo,
            QuantidadePedidos = p.Quantidade,
            Valor = p.Valor,
            Percentual = hoje.Valor == 0m ? 0m : Math.Round(p.Valor / hoje.Valor * 100m, 2)
        }).OrderByDescending(p => p.Valor).ToList();
        // Converte enumerações de atendimento em rótulos compreensíveis para a tela.
        var tiposAtendimento = atendimentosHoje.Select(a => new TipoAtendimentoGraficoViewModel
        {
            Rotulo = RotuloAtendimento(a.Tipo),
            QuantidadePedidos = a.Quantidade,
            Valor = a.Valor,
            Percentual = hoje.Valor == 0m ? 0m : Math.Round(a.Valor / hoje.Valor * 100m, 2)
        }).OrderByDescending(a => a.QuantidadePedidos).ToList();

        // Cria os pontos dos últimos sete dias, preenchendo dias sem venda com zero.
        var ultimosSeteDias = await CriarSerieDiariaAsync(vendasValidas, inicioHoje.AddDays(-6), fimHoje);
        // Cria os pontos do primeiro dia do mês até hoje, sem adicionar dias futuros.
        var diasDoMes = await CriarSerieDiariaAsync(vendasValidas, inicioMes, fimHoje);
        // Agrupa vendas do dia pela hora de criação do pedido para identificar picos de movimento.
        var horariosPico = await CriarHorariosPicoAsync(vendasValidas, inicioHoje, fimHoje);

        // Carrega os rankings independentes por período usando snapshots dos itens vendidos.
        var rankings = new RankingsProdutosViewModel
        {
            Hoje = await CriarRankingProdutosAsync(vendasValidas, inicioHoje, fimHoje),
            Semana = await CriarRankingProdutosAsync(vendasValidas, inicioSemana, fimHoje),
            Mes = await CriarRankingProdutosAsync(vendasValidas, inicioMes, fimHoje)
        };
        // Monta os resumos de semana e mês a partir de suas próprias séries e grupos persistidos.
        var resumoSemana = await CriarResumoPeriodoAsync(vendasValidas, inicioSemana, fimHoje, semana, await CriarSerieDiariaAsync(vendasValidas, inicioSemana, fimHoje), rankings.Semana);
        var resumoMes = await CriarResumoPeriodoAsync(vendasValidas, inicioMes, fimHoje, mes, diasDoMes, rankings.Mes);
        // Consulta o estado operacional sem modificar comandas, mesas ou pedidos abertos.
        var fechamento = await CriarStatusFechamentoAsync(empresaId, inicioHoje, fimHoje);

        // Retorna apenas ViewModels consolidados, sem entidades e sem expor EmpresaId.
        return new DashboardRelatorioViewModel
        {
            Slug = slug,
            NomeEmpresa = nomeEmpresa,
            AtualizadoEm = agora,
            Cards = new DashboardCardsViewModel
            {
                VendasHoje = hoje.Valor,
                VendasSemana = semana.Valor,
                VendasMes = mes.Valor,
                PedidosHoje = hoje.Quantidade,
                TicketMedioHoje = hoje.TicketMedio,
                DeliveryHoje = atendimentosHoje.Where(a => a.Tipo == TipoAtendimento.Entrega).Sum(a => a.Quantidade),
                RetiradaHoje = atendimentosHoje.Where(a => a.Tipo == TipoAtendimento.Retirada).Sum(a => a.Quantidade),
                MesasHoje = atendimentosHoje.Where(a => a.Tipo == TipoAtendimento.Mesa).Sum(a => a.Quantidade),
                PedidosManuaisHoje = pedidosManuaisHoje,
                TaxasEntregaHoje = hoje.TaxaEntrega,
                TaxasServicoHoje = taxasServicoHoje,
                DescontosHoje = 0m,
                CancelamentosHoje = cancelamentosHoje
            },
            Graficos = new DashboardGraficosViewModel { UltimosSeteDias = ultimosSeteDias, DiasDoMes = diasDoMes, FormasPagamentoHoje = formasPagamento, TiposAtendimentoHoje = tiposAtendimento, HorariosPicoHoje = horariosPico },
            ProdutosMaisVendidos = rankings,
            ResumoSemana = resumoSemana,
            ResumoMes = resumoMes,
            StatusFechamento = fechamento
        };
    }

    // Agrega valor, quantidade e taxa de entrega no banco para o intervalo semiaberto informado.
    private async Task<(decimal Valor, int Quantidade, decimal TaxaEntrega, decimal TicketMedio)> ResumirAsync(IQueryable<Models.Pedido> vendas, DateTime inicio, DateTime fim)
    {
        // Executa uma única agregação SQL em vez de materializar pedidos no servidor.
        var resumo = await vendas.Where(p => p.DataCriacao >= inicio && p.DataCriacao < fim).GroupBy(_ => 1)
            .Select(g => new { Valor = g.Sum(p => p.Total), Quantidade = g.Count(), TaxaEntrega = g.Sum(p => p.TaxaEntrega) }).FirstOrDefaultAsync();
        // Evita divisão por zero quando o período ainda não possui vendas.
        var quantidade = resumo?.Quantidade ?? 0;
        var valor = resumo?.Valor ?? 0m;
        // Retorna zeros seguros para os cards e gráficos vazios.
        return (valor, quantidade, resumo?.TaxaEntrega ?? 0m, quantidade == 0 ? 0m : valor / quantidade);
    }

    // Constrói uma série diária contínua, mantendo dias sem movimento visíveis com valores zero.
    private async Task<List<VendaPorDiaViewModel>> CriarSerieDiariaAsync(IQueryable<Models.Pedido> vendas, DateTime inicio, DateTime fim)
    {
        // Agrupa no banco por data do pedido para minimizar dados transferidos.
        var agrupados = await vendas.Where(p => p.DataCriacao >= inicio && p.DataCriacao < fim).GroupBy(p => p.DataCriacao.Date)
            .Select(g => new { Data = g.Key, Valor = g.Sum(p => p.Total), Quantidade = g.Count() }).ToListAsync();
        // Indexa os grupos para preencher rapidamente cada data do período.
        var porData = agrupados.ToDictionary(g => g.Data, g => g);
        // Cria a sequência de datas inclusive no início e exclusiva no fim.
        return Enumerable.Range(0, (fim.Date - inicio.Date).Days).Select(indice => inicio.Date.AddDays(indice)).Select(data =>
        {
            // Recupera o grupo existente ou usa zero no dia sem pedidos concluídos.
            var grupo = porData.GetValueOrDefault(data);
            var quantidade = grupo?.Quantidade ?? 0;
            var valor = grupo?.Valor ?? 0m;
            // Expõe o ponto pronto para Chart.js, incluindo ticket médio calculado no servidor.
            return new VendaPorDiaViewModel { Rotulo = data.ToString("dd/MM"), Valor = valor, QuantidadePedidos = quantidade, TicketMedio = quantidade == 0 ? 0m : valor / quantidade };
        }).ToList();
    }

    // Agrupa pedidos concluídos por hora para o gráfico de movimento do dia.
    private async Task<List<HorarioPicoViewModel>> CriarHorariosPicoAsync(IQueryable<Models.Pedido> vendas, DateTime inicio, DateTime fim)
    {
        // Agrupa por hora no banco e ordena cronologicamente.
        var grupos = await vendas.Where(p => p.DataCriacao >= inicio && p.DataCriacao < fim).GroupBy(p => p.DataCriacao.Hour)
            .Select(g => new { Hora = g.Key, Valor = g.Sum(p => p.Total), Quantidade = g.Count() }).OrderBy(g => g.Hora).ToListAsync();
        // Converte cada grupo em uma representação própria da View.
        return grupos.Select(g => new HorarioPicoViewModel { Rotulo = $"{g.Hora:00}h", Valor = g.Valor, QuantidadePedidos = g.Quantidade, TicketMedio = g.Quantidade == 0 ? 0m : g.Valor / g.Quantidade }).ToList();
    }

    // Calcula o Top 5 usando os snapshots de itens e os pedidos válidos do período.
    private async Task<List<ProdutoMaisVendidoViewModel>> CriarRankingProdutosAsync(IQueryable<Models.Pedido> vendas, DateTime inicio, DateTime fim) => await vendas
        .Where(p => p.DataCriacao >= inicio && p.DataCriacao < fim).SelectMany(p => p.Itens).GroupBy(i => i.NomeProdutoSnapshot)
        .OrderByDescending(g => g.Sum(i => i.Quantidade)).ThenBy(g => g.Key).Take(5)
        .Select(g => new ProdutoMaisVendidoViewModel { Nome = g.Key, Quantidade = g.Sum(i => i.Quantidade), Faturamento = g.Sum(i => i.Subtotal) }).ToListAsync();

    // Monta indicadores de resumo consultando o período já filtrado pelo tenant.
    private async Task<ResumoPeriodoViewModel> CriarResumoPeriodoAsync(IQueryable<Models.Pedido> vendas, DateTime inicio, DateTime fim, (decimal Valor, int Quantidade, decimal TaxaEntrega, decimal TicketMedio) resumo, List<VendaPorDiaViewModel> dias, List<ProdutoMaisVendidoViewModel> produtos)
    {
        // Busca a forma mais usada do período pelo snapshot de pagamento.
        var pagamento = await vendas.Where(p => p.DataCriacao >= inicio && p.DataCriacao < fim).GroupBy(p => string.IsNullOrEmpty(p.NomeFormaPagamentoSnapshot) ? "Não informado" : p.NomeFormaPagamentoSnapshot)
            .OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefaultAsync();
        // Busca o atendimento mais frequente do período usando o enum armazenado no pedido.
        var atendimento = await vendas.Where(p => p.DataCriacao >= inicio && p.DataCriacao < fim).GroupBy(p => p.TipoAtendimento)
            .OrderByDescending(g => g.Count()).Select(g => (TipoAtendimento?)g.Key).FirstOrDefaultAsync();
        // Identifica o melhor dia sem fabricar resultado quando não há movimento.
        var melhorDia = dias.OrderByDescending(d => d.Valor).FirstOrDefault(d => d.Valor > 0m);
        // Divide pelos dias transcorridos do intervalo, mantendo média zero quando aplicável.
        var diasDecorridos = Math.Max(1, (fim.Date - inicio.Date).Days);
        return new ResumoPeriodoViewModel { TotalVendido = resumo.Valor, Pedidos = resumo.Quantidade, TicketMedio = resumo.TicketMedio, MediaDiaria = resumo.Valor / diasDecorridos, MelhorDia = melhorDia?.Rotulo ?? "Sem movimento", FormaPagamentoMaisUsada = pagamento ?? "Não informado", ProdutoMaisVendido = produtos.FirstOrDefault()?.Nome ?? "Sem vendas", TipoAtendimentoMaisUsado = atendimento.HasValue ? RotuloAtendimento(atendimento.Value) : "Não informado" };
    }

    // Consulta contagens operacionais apenas para informar a decisão de fechamento ao atendente.
    private async Task<StatusFechamentoViewModel> CriarStatusFechamentoAsync(int empresaId, DateTime inicioHoje, DateTime fimHoje)
    {
        // Conta pedidos ainda não entregues nem cancelados da empresa.
        var pedidosAbertos = await _context.Pedidos.AsNoTracking().CountAsync(p => p.EmpresaId == empresaId && p.Status != StatusPedido.Entregue && p.Status != StatusPedido.Cancelado && !p.Cancelado);
        // Conta comandas abertas ou aguardando pagamento da empresa.
        var comandasAbertas = await _context.Comandas.AsNoTracking().CountAsync(c => c.EmpresaId == empresaId && (c.Status == StatusComanda.Aberta || c.Status == StatusComanda.AguardandoPagamento));
        // Conta mesas ocupadas sem assumir que elas podem ser fechadas automaticamente.
        var mesasOcupadas = await _context.Mesas.AsNoTracking().CountAsync(m => m.EmpresaId == empresaId && m.Status == StatusMesa.Ocupada);
        // Conta pedidos do dia sem pagamento informado como alerta operacional.
        var semPagamento = await _context.Pedidos.AsNoTracking().CountAsync(p => p.EmpresaId == empresaId && p.DataCriacao >= inicioHoje && p.DataCriacao < fimHoje && p.Status == StatusPedido.Entregue && !p.Cancelado && p.FormaPagamentoId == null);
        // Escolhe um rótulo sem acionar nenhuma rotina de fechamento.
        var situacao = pedidosAbertos > 0 || comandasAbertas > 0 || mesasOcupadas > 0 ? "Movimento em andamento" : "Pronto para fechamento";
        return new StatusFechamentoViewModel { Situacao = situacao, PedidosAbertos = pedidosAbertos, ComandasAbertas = comandasAbertas, MesasOcupadas = mesasOcupadas, PedidosSemFormaPagamento = semPagamento };
    }

    // Traduz os valores reais do enum em rótulos de negócio para o dashboard.
    private static string RotuloAtendimento(TipoAtendimento tipo) => tipo switch { TipoAtendimento.Entrega => "Delivery", TipoAtendimento.Retirada => "Retirada", TipoAtendimento.Mesa => "Mesa", _ => "Não informado" };
}
