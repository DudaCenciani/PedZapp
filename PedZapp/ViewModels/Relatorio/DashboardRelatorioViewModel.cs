namespace PedZapp.ViewModels.Relatorio;

/// <summary>
/// Reúne dados financeiros calculados no servidor para o dashboard automático da empresa.
/// Nenhuma entidade ou identificador interno de empresa é enviado à View.
/// </summary>
public sealed class DashboardRelatorioViewModel
{
    // Mantém o slug necessário para rotas administrativas seguras da empresa atual.
    public string Slug { get; init; } = string.Empty;
    // Identifica a empresa no cabeçalho sem expor seu identificador interno.
    public string NomeEmpresa { get; init; } = string.Empty;
    // Registra a data e hora em que o servidor consolidou este painel.
    public DateTime AtualizadoEm { get; init; }
    // Contém os indicadores de hoje, semana e mês calculados automaticamente.
    public DashboardCardsViewModel Cards { get; init; } = new();
    // Contém séries e distribuições usadas pelos gráficos do dashboard.
    public DashboardGraficosViewModel Graficos { get; init; } = new();
    // Exibe o ranking de produtos para os três atalhos de período disponíveis na tela.
    public RankingsProdutosViewModel ProdutosMaisVendidos { get; init; } = new();
    // Resume o período semanal atual para o bloco visual correspondente.
    public ResumoPeriodoViewModel ResumoSemana { get; init; } = new();
    // Resume o período mensal atual para o bloco visual correspondente.
    public ResumoPeriodoViewModel ResumoMes { get; init; } = new();
    // Informa a situação operacional do dia sem executar fechamento automático.
    public StatusFechamentoViewModel StatusFechamento { get; init; } = new();
}

/// <summary>Expõe valores principais do período atual e os indicadores operacionais de hoje.</summary>
public sealed class DashboardCardsViewModel
{
    public decimal VendasHoje { get; init; }
    public decimal VendasSemana { get; init; }
    public decimal VendasMes { get; init; }
    public int PedidosHoje { get; init; }
    public decimal TicketMedioHoje { get; init; }
    public int DeliveryHoje { get; init; }
    public int RetiradaHoje { get; init; }
    public int MesasHoje { get; init; }
    public int PedidosManuaisHoje { get; init; }
    public decimal TaxasEntregaHoje { get; init; }
    public decimal TaxasServicoHoje { get; init; }
    public decimal DescontosHoje { get; init; }
    public int CancelamentosHoje { get; init; }
}

/// <summary>Organiza os conjuntos de dados usados pelos gráficos e estados vazios da página.</summary>
public sealed class DashboardGraficosViewModel
{
    public List<VendaPorDiaViewModel> UltimosSeteDias { get; init; } = [];
    public List<VendaPorDiaViewModel> DiasDoMes { get; init; } = [];
    public List<FormaPagamentoGraficoViewModel> FormasPagamentoHoje { get; init; } = [];
    public List<TipoAtendimentoGraficoViewModel> TiposAtendimentoHoje { get; init; } = [];
    public List<HorarioPicoViewModel> HorariosPicoHoje { get; init; } = [];
}

/// <summary>Representa um ponto diário já consolidado pelo banco de dados.</summary>
public sealed class VendaPorDiaViewModel
{
    public string Rotulo { get; init; } = string.Empty;
    public decimal Valor { get; init; }
    public int QuantidadePedidos { get; init; }
    public decimal TicketMedio { get; init; }
}

/// <summary>Representa a participação de uma forma de pagamento no dia.</summary>
public sealed class FormaPagamentoGraficoViewModel
{
    public string Rotulo { get; init; } = string.Empty;
    public decimal Valor { get; init; }
    public int QuantidadePedidos { get; init; }
    public decimal Percentual { get; init; }
}

/// <summary>Representa a participação de um tipo de atendimento no dia.</summary>
public sealed class TipoAtendimentoGraficoViewModel
{
    public string Rotulo { get; init; } = string.Empty;
    public decimal Valor { get; init; }
    public int QuantidadePedidos { get; init; }
    public decimal Percentual { get; init; }
}

/// <summary>Representa o movimento financeiro e a quantidade de pedidos de uma hora.</summary>
public sealed class HorarioPicoViewModel
{
    public string Rotulo { get; init; } = string.Empty;
    public decimal Valor { get; init; }
    public int QuantidadePedidos { get; init; }
    public decimal TicketMedio { get; init; }
}

/// <summary>Representa um produto vendido usando o snapshot histórico do item do pedido.</summary>
public sealed class ProdutoMaisVendidoViewModel
{
    public string Nome { get; init; } = string.Empty;
    public int Quantidade { get; init; }
    public decimal Faturamento { get; init; }
}

/// <summary>Disponibiliza rankings por hoje, semana e mês sem precisar de filtros iniciais.</summary>
public sealed class RankingsProdutosViewModel
{
    public List<ProdutoMaisVendidoViewModel> Hoje { get; init; } = [];
    public List<ProdutoMaisVendidoViewModel> Semana { get; init; } = [];
    public List<ProdutoMaisVendidoViewModel> Mes { get; init; } = [];
}

/// <summary>Resume um período automático para os blocos semanal e mensal.</summary>
public sealed class ResumoPeriodoViewModel
{
    public decimal TotalVendido { get; init; }
    public int Pedidos { get; init; }
    public decimal TicketMedio { get; init; }
    public decimal MediaDiaria { get; init; }
    public string MelhorDia { get; init; } = "Sem movimento";
    public string FormaPagamentoMaisUsada { get; init; } = "Não informado";
    public string ProdutoMaisVendido { get; init; } = "Sem vendas";
    public string TipoAtendimentoMaisUsado { get; init; } = "Não informado";
}

/// <summary>Expõe o estado atual de fechamento sem alterar o fluxo já existente de comandas.</summary>
public sealed class StatusFechamentoViewModel
{
    public string Situacao { get; init; } = "Movimento em andamento";
    public int PedidosAbertos { get; init; }
    public int ComandasAbertas { get; init; }
    public int MesasOcupadas { get; init; }
    public int PedidosSemFormaPagamento { get; init; }
}
