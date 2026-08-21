namespace PedZapp.Enums;

/// <summary>Define a sequência operacional de pedidos escolhida por cada empresa.</summary>
public enum TipoFluxoPedido
{
    // Mantém o fluxo atual como padrão para não alterar empresas existentes.
    Completo = 1,
    // Oculta etapas intermediárias e usa confirmação, preparo e finalização.
    Simplificado = 2
}
