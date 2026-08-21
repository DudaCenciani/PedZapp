namespace PedZapp.Services
{
    /// <summary>Encapsula a chamada oficial da Meta, sem expor credenciais a controllers ou Views.</summary>
    public interface IWhatsAppCloudService
    {
        Task<WhatsAppEnvioResultado> EnviarConfirmacaoAsync(string telefone, string nomeCliente, string numeroPedido, string previsao, string nomeEmpresa, CancellationToken cancellationToken = default);
    }
    public sealed record WhatsAppEnvioResultado(bool Sucesso, int? StatusHttp);
}
