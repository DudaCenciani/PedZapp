using PedZapp.Enums;
using PedZapp.ViewModels.Pedido;

namespace PedZapp.Services
{
    public interface IPedidoPrintService
    {
        Task<SolicitacaoImpressaoResultado> CriarParaConfirmacaoAsync(int pedidoId, int empresaId);
        Task<SolicitacaoImpressaoResultado> CriarReimpressaoAsync(int pedidoId, int empresaId, TipoImpressao tipo);
        Task<PedidoImpressaoViewModel?> ObterParaImpressaoAsync(int empresaId, string codigoPublico, string tokenPublico);
        Task<bool> RegistrarTentativaAsync(int empresaId, string codigoPublico, string tokenPublico);
    }
}
