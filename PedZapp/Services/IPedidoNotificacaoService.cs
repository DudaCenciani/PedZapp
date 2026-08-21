using PedZapp.Models;

namespace PedZapp.Services
{
    /// <summary>
    /// Publica avisos pós-commit para pedidos externos já persistidos.
    /// </summary>
    public interface IPedidoNotificacaoService
    {
        Task NotificarNovoPedidoAsync(Pedido pedido, string slug);

        /// <summary>
        /// Publica um aviso visual fictício somente quando uma action protegida em Development o solicita.
        /// </summary>
        Task NotificarTesteAsync(int empresaId, string slug);
    }
}
