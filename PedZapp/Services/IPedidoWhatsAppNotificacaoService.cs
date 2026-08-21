namespace PedZapp.Services
{
    /// <summary>
    /// Orquestra a confirmação de WhatsApp após o pedido já ter sido confirmado no banco.
    /// A interface mantém a comunicação externa fora do controller e permite evoluir para outros eventos.
    /// </summary>
    public interface IPedidoWhatsAppNotificacaoService
    {
        Task<WhatsAppNotificacaoResultado> EnviarConfirmacaoAsync(int pedidoId, int empresaId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Resultado administrativo, sem dados sensíveis da mensagem ou das credenciais da Meta.
    /// </summary>
    public sealed record WhatsAppNotificacaoResultado(bool Enviado, bool Ignorado, string? Mensagem);
}
