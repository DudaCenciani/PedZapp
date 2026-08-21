namespace PedZapp.ViewModels.Shared;

/// <summary>
/// Transporta somente informações de apresentação para o cabeçalho reutilizável das páginas administrativas.
/// O slug é usado exclusivamente para montar o link de retorno ao painel da própria empresa.
/// </summary>
public sealed class CabecalhoAdministrativoViewModel
{
    // Mantém o slug já resolvido pela página para preservar a rota segura de retorno.
    public string Slug { get; init; } = string.Empty;
    // Exibe um ícone textual acompanhado por título e descrição acessíveis.
    public string Icone { get; init; } = "◈";
    // Define o título principal da área administrativa.
    public string Titulo { get; init; } = string.Empty;
    // Explica brevemente a responsabilidade da tela sem alterar qualquer regra de negócio.
    public string Descricao { get; init; } = string.Empty;
    // Opcionalmente informa uma ação disponível, mantida como conteúdo de apresentação.
    public string? AcaoTexto { get; init; }
    // Define o destino existente da ação principal quando a tela o possui.
    public string? AcaoUrl { get; init; }
}
