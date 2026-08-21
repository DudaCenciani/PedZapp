namespace PedZapp.ViewModels.PainelEmpresa;

/// <summary>
/// Representa uma ação administrativa gerada a partir de dados reais da empresa atual.
/// Não transporta entidades nem identificadores internos para a View.
/// </summary>
public sealed class PendenciaEmpresaViewModel
{
    public string Titulo { get; init; } = string.Empty;
    public string Descricao { get; init; } = string.Empty;
    public string UrlResolucao { get; init; } = string.Empty;
    public string TextoBotao { get; init; } = "Resolver";
    public PrioridadePendenciaEmpresa Prioridade { get; init; }
}

/// <summary>Resultado compacto consumido pelo dashboard para exibir somente as pendências prioritárias.</summary>
public sealed class PendenciasEmpresaResultadoViewModel
{
    public int Total { get; init; }
    public int OutrasPendencias { get; init; }
    public IReadOnlyList<PendenciaEmpresaViewModel> Pendencias { get; init; } = [];
}

public enum PrioridadePendenciaEmpresa
{
    Alta = 1,
    Media = 2,
    Baixa = 3
}
